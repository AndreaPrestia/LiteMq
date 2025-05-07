using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;

namespace LiteMq.Managers;

internal class SubscriptionManager
{
    private readonly ConcurrentDictionary<string, List<Subscription>> _subscribers = new();
    private readonly ConcurrentDictionary<string, int> _topicIndex = new();
    private readonly ILogger<SubscriptionManager> _logger = new Logger<SubscriptionManager>(new LoggerFactory());

    private delegate void NotifyMessagePublish(object sender, string topic, string payload);

    private event NotifyMessagePublish? NotifySubscribers;

    public SubscriptionManager()
    {
        NotifySubscribers += OnNotifySubscribers;
    }
    
    /// <summary>
    /// Subscribes and listens to incoming messages on a queue
    /// </summary>
    /// <param name="topic"></param>
    /// <param name="client"></param>
    /// <param name="exclusive"></param>
    /// <returns></returns>
    public void Subscribe(string topic, TcpClient client, bool exclusive)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ArgumentNullException.ThrowIfNull(client);

        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            Client = client,
            Exclusive = exclusive,
            Created = DateTime.UtcNow
        };

        var topicTrimmedLowered = topic.Trim().ToLowerInvariant();

        if (!_subscribers.ContainsKey(topicTrimmedLowered))
            _subscribers[topic] = new List<Subscription>();
        _subscribers[topic].Add(subscription);

        if (!_topicIndex.ContainsKey(topicTrimmedLowered))
        {
            var addedTopicToIndexResult = _topicIndex.TryAdd(topicTrimmedLowered, 0);
            _logger.LogDebug("Add topic '{topic}' to index result: {addedTopicToIndexResult}", topicTrimmedLowered,
                addedTopicToIndexResult);
        }

        _logger.LogInformation(
            "Subscribed '{id}' to queue '{queue}' at {timestamp}", subscription.Id, topicTrimmedLowered,
            subscription.Created.ToShortDateString());

        //NotifySubscribe?.Invoke(this, topicTrimmedLowered);
    }

    /// <summary>
    /// Gets the next round-robin load balanced subscription for the specified topic. If an exclusive one is provided only that will be returned.
    /// </summary>
    /// <param name="topic"></param>
    /// <returns></returns>
    public Subscription? GetNextSubscription(string topic)
    {
        if (_subscribers.IsEmpty)
        {
            _logger.LogWarning("No subscriptions available.");
            return null;
        }

        if (!_subscribers.TryGetValue(topic, out var subscriptions))
        {
            _logger.LogWarning("No subscriptions available for topic '{topic}'.", topic);
            return null;
        }

        if (subscriptions == null! || subscriptions.Count == 0)
        {
            _logger.LogWarning("No subscriptions for topic '{topic}'. No processing will be done.", topic);
            return null;
        }

        var exclusiveSubscription = subscriptions.FirstOrDefault(x => x.Exclusive);

        if (exclusiveSubscription != null)
        {
            _logger.LogDebug("Found exclusive subscription '{subscriptionId}' for queue '{queue}'",
                exclusiveSubscription.Id, topic);
            return exclusiveSubscription;
        }

        if (!_topicIndex.TryGetValue(topic, out var currentIndex))
        {
            currentIndex = 0;
            _topicIndex[topic] = currentIndex;
        }

        var subscription = subscriptions.ElementAt(currentIndex);

        if (subscription == null!)
        {
            _logger.LogWarning("No subscription found for topic '{topic}' at index {currentIndex}", topic,
                currentIndex);
            return null;
        }

        _logger.LogDebug("Found subscription for topic '{topic}' for index {currentIndex}", topic, currentIndex);

        _topicIndex[topic] = (currentIndex + 1) % subscriptions.Count;

        _logger.LogDebug("New calculated index for next iteration for topic '{topic}' {currentIndex}", topic,
            _topicIndex[topic]);

        return subscription;
    }
    
    public void NotifyConnectedSubscribers(string topic, string payload)
    {
        NotifySubscribers?.Invoke(this, topic, payload);
    }
    
    private void OnNotifySubscribers(object sender, string topic, string payload)
    {
        if (_subscribers.TryGetValue(topic, out var subscriptions))
        {
            foreach (var subscription in subscriptions.ToList())
            {
                try
                {
                    if (subscription.Client is { Connected: true })
                    {
                        var stream = subscription.Client.GetStream();
                        var data = Encoding.UTF8.GetBytes(payload + "\n");
                        stream.Write(data, 0, data.Length);
                    }
                }
                catch
                {
                    subscriptions.Remove(subscription);
                }
            }
        }
    }
}