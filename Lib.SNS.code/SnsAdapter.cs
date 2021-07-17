using Amazon.Runtime;
using Amazon.SimpleNotificationService.Model;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Amazon.SimpleNotificationService {

  public partial class SnsAdapter {

    private const string AwsMagicArnForPendingSubscription = "PendingConfirmation";

    private Dictionary<string, string> _ResolveCache = new Dictionary<string, string>();

    public SnsAdapter(string awsAccessKey, string awsSecretKey, string subscriptionCallbackUrl, string awsRegionName = null) {

      if (string.IsNullOrWhiteSpace(awsRegionName)) {
        this.AwsRegion = RegionEndpoint.EUCentral1;
      }
      else {
        this.AwsRegion = RegionEndpoint.EnumerableAllRegions.Where(r => String.Equals(r.SystemName, awsRegionName, StringComparison.InvariantCultureIgnoreCase)).Single();
      }

      this.AwsRegionName = this.AwsRegion.SystemName;
      this.AwsCredentials = new BasicAWSCredentials(awsAccessKey, awsSecretKey);
      this.SubscriptionCallbackUrl = subscriptionCallbackUrl;
      this.ReloadExistingSubscriptions();
    }

#if DEBUG
    public static bool SuppressSubscriptionDevmode { get; set; } = false;
#endif

    private AWSCredentials AwsCredentials { get; set; }
    public RegionEndpoint AwsRegion { get; private set; }
    public String AwsRegionName { get; private set; }

    public String SubscriptionCallbackUrl { get; private set; }
    public Dictionary<string, Func<string>> TopicParameterKeyPlaceholderResolvers { get; set; } = new Dictionary<string, Func<string>>();

    public void ReloadExistingSubscriptions() {

      var subscriptions = new List<SubscriptionInfo>();
      using (var client = new AmazonSimpleNotificationServiceClient(this.AwsCredentials, this.AwsRegion)) {

        var request = new ListTopicsRequest();
        ListTopicsResponse response;
        Task<ListTopicsResponse> ltt;

        do {

          ltt = client.ListTopicsAsync(request);
          ltt.Wait();
          response = ltt.Result;

          foreach (var topic in response.Topics) {

            var lsRequest = new ListSubscriptionsByTopicRequest() { TopicArn = topic.TopicArn };
            ListSubscriptionsByTopicResponse lsResponse;
            Task<ListSubscriptionsByTopicResponse> lst;

            do {
              lst = client.ListSubscriptionsByTopicAsync(lsRequest);
              lst.Wait();
              lsResponse = lst.Result;

              //íterate all subscriptions which are bound to our SubscriptionCallbackUrl...
              foreach (var subscription in lsResponse.Subscriptions.Where(s => string.Equals(s.Endpoint, this.SubscriptionCallbackUrl, StringComparison.InvariantCultureIgnoreCase))) {
                
                //skip pending subscriptions...
                if (!subscription.SubscriptionArn.Equals(AwsMagicArnForPendingSubscription)){

                  subscriptions.Add(new SubscriptionInfo(this, subscription.TopicArn, subscription.SubscriptionArn, false));

                }

              }

              lsRequest.NextToken = lsResponse.NextToken;

            } while (!string.IsNullOrEmpty(lsResponse.NextToken));
          }

          request.NextToken = response.NextToken;

        } while (!string.IsNullOrEmpty(response.NextToken));

        lock (_ConfirmedSubscriptions) {
          _ConfirmedSubscriptions = subscriptions;
        }

        lock (_LocalPendingSubscriptions) {
          foreach (var lps in _LocalPendingSubscriptions.ToArray()) {
            if (subscriptions.Where(s => (s.SubscriptionArn ?? "") == (lps.SubscriptionArn ?? "")).Any()) {
              _LocalPendingSubscriptions.Remove(lps);
            }
          }
        }

      }
    }

    private List<SubscriptionInfo> _ConfirmedSubscriptions = new List<SubscriptionInfo>();
    private List<SubscriptionInfo> _LocalPendingSubscriptions = new List<SubscriptionInfo>();

    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public SubscriptionInfo[] Subscriptions {
      get {
        lock (_ConfirmedSubscriptions) {
          return _ConfirmedSubscriptions.ToArray();
        }
      }
    }

    public SubscriptionInfo GetOrSubscribe(string topicArn) {
      SubscriptionInfo item;

      lock (_ConfirmedSubscriptions) {
        item = _ConfirmedSubscriptions.Where(s => string.Equals(s.TopicArn, topicArn, StringComparison.InvariantCultureIgnoreCase)).SingleOrDefault();
        if (item is object) {
          return item;
        }
      }

      lock (_LocalPendingSubscriptions) {
        item = _LocalPendingSubscriptions.Where(s => string.Equals(s.TopicArn, topicArn, StringComparison.InvariantCultureIgnoreCase)).SingleOrDefault();
        if (item is object) {
          return item;
        }

#if DEBUG
        if (SuppressSubscriptionDevmode) {
          item = new SubscriptionInfo(this, topicArn, "<dummy>", false); //simulate already confirmed subscription
          _LocalPendingSubscriptions.Add(item);
          return item;
        }
#endif

        using (var client = new AmazonSimpleNotificationServiceClient(this.AwsCredentials, this.AwsRegion)) {
          Task<SubscribeResponse> t;
          if (this.SubscriptionCallbackUrl.StartsWith("https:")) {
            t = client.SubscribeAsync(topicArn, "https", this.SubscriptionCallbackUrl);
          }
          else {
            t = client.SubscribeAsync(topicArn, "http", this.SubscriptionCallbackUrl);
          }

          t.Wait();

          if (t.IsCompleted && t.Result is object) {
            item = new SubscriptionInfo(this, topicArn, t.Result.SubscriptionArn, true);
            _LocalPendingSubscriptions.Add(item);
          }
        }
      }

      return item;
    }

    public void UnSubscribeAll() {
      bool reloadRequired = false;
      using (var client = new AmazonSimpleNotificationServiceClient(this.AwsCredentials, this.AwsRegion)) {
        Task<UnsubscribeResponse> t;
        foreach (var s in this.Subscriptions) {
          t = client.UnsubscribeAsync(s.SubscriptionArn);
          t.Wait();
          if (t.IsCompleted) {
            reloadRequired = true;
          }
        }

        if (reloadRequired) {
          this.ReloadExistingSubscriptions();
        }
      }
    }

    private void UnSubscribe(string subscriptionArn) {
      using (var client = new AmazonSimpleNotificationServiceClient(this.AwsCredentials, this.AwsRegion)) {
        Task<UnsubscribeResponse> t;
        t = client.UnsubscribeAsync(subscriptionArn);
        t.Wait();
        if (t.IsCompleted) {
          this.ReloadExistingSubscriptions();
        }
      }
    }

    public void Confirm(string topicArn, string token) {
      using (var client = new AmazonSimpleNotificationServiceClient(this.AwsCredentials, this.AwsRegion)) {
        var cr = new ConfirmSubscriptionRequest();
        cr.TopicArn = topicArn;
        cr.Token = token;
        var t = client.ConfirmSubscriptionAsync(cr);
        t.Wait();
        if (t.IsCompleted && t.Result is object) {
          this.ReloadExistingSubscriptions();
        }
      }
    }


    internal string ResolveToValidTopcArn(string topicArnOrParameterKey) {
      if (topicArnOrParameterKey.StartsWith("arn:")) {
        return topicArnOrParameterKey;
      }

      lock (_ResolveCache) {
        foreach (var phName in this.TopicParameterKeyPlaceholderResolvers.Keys) {
          string pt = "{" + phName + "}";
          if (topicArnOrParameterKey.Contains(pt)) {
            string pv = this.TopicParameterKeyPlaceholderResolvers[phName].Invoke();
            topicArnOrParameterKey = topicArnOrParameterKey.Replace(pt, pv);
          }
        }

        if (_ResolveCache.ContainsKey(topicArnOrParameterKey)) {
          return _ResolveCache[topicArnOrParameterKey];
        }

        if (topicArnOrParameterKey.Contains("{")) {
          throw new Exception($"There is a unresolvable Placeholder within ParameterKey '{topicArnOrParameterKey}'!");
        }

        using (var client = new AmazonSimpleSystemsManagementClient(this.AwsCredentials, this.AwsRegion)) {
          var request = new GetParameterRequest() { Name = topicArnOrParameterKey };
          var t = client.GetParameterAsync(request);
          t.Wait();
          if (t.IsCompleted && t.Result is object && t.Result.Parameter is object) {
            _ResolveCache.Add(topicArnOrParameterKey, t.Result.Parameter.Value);
            return t.Result.Parameter.Value;
          }
        }
      }

      return null;
    }

    public event IncommingRawMessageEventHandler IncommingRawMessage;

    public delegate void IncommingRawMessageEventHandler(SubscriptionInfo ep, string rawMessage);

    public void Receive(string topicArn, string rawMessage) {
      Trace.WriteLine($"Incomming SNS-Message for Topic '{topicArn}'");
      if (IncommingRawMessage is object) {
        SubscriptionInfo ep;
        lock (_ConfirmedSubscriptions) {
          ep = _ConfirmedSubscriptions.Where(s => string.Equals(s.TopicArn, topicArn, StringComparison.InvariantCultureIgnoreCase)).SingleOrDefault();
        }
        IncommingRawMessage?.Invoke(ep, rawMessage);
      }
    }

    /// <summary>
    /// returns nothing on error
    /// </summary>
    public string PublishRawMessage(string topicArn, string rawMessage) {
      try {
        using (var client = new AmazonSimpleNotificationServiceClient(this.AwsCredentials, this.AwsRegion)) {
          var pr = new PublishRequest();
          pr.TopicArn = topicArn;
          pr.Message = rawMessage;
          var t = client.PublishAsync(pr);
          t.Wait();
          if (t.IsCompleted && t.Result is object) {
            Trace.TraceInformation ($"Outgoing SNS-Message for Topic '{topicArn}' Msg-ID: {t.Result.MessageId}");
            return t.Result.MessageId;
          }
          else {
            Trace.TraceError($"Publish SNS-Message for Topic '{topicArn}' FAILED: AWS responded non-success");
            return null;
          }
        }
      }
      catch (Exception ex) {
        Trace.TraceError($"Publish SNS-Message for Topic '{topicArn}' EXCEPTION: {ex.Message}");
        return null;
      }
    }

    public void ResubscribeBoundObjects() {
      lock (_BoundObjects) {
        foreach (var bindingAdapter in _BoundObjects.Values) {
          bindingAdapter.Dispose();
          bindingAdapter.Wireup();
        }
      }
    }

    private Dictionary<object, ObjectBindingAdapter> _BoundObjects = new Dictionary<object, ObjectBindingAdapter>();

    public void BindObject(object obj) {
      lock (_BoundObjects) {
        if (_BoundObjects.ContainsKey(obj)) {
          return;
        }

        _BoundObjects.Add(obj, new ObjectBindingAdapter(obj, this));
      }
    }

    public void UnbindObject(object obj) {
      lock (_BoundObjects) {
        if (_BoundObjects.ContainsKey(obj)) {
          _BoundObjects[obj].Dispose();
          _BoundObjects.Remove(obj);
        }
      }
    }

  }

}
