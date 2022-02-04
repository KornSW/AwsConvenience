using System;
using System.Diagnostics;

namespace Amazon.SimpleNotificationService {

  partial class SnsAdapter {

    [DebuggerDisplay("Subscription: {TopicArn}")]
    public partial class SubscriptionInfo {

      public SubscriptionInfo(SnsAdapter parentEndpoint, string topicArn, string subscriptionArn, bool isLocalPending) {
        this.ParentEndpoint = parentEndpoint;
        this.TopicArn = topicArn;
        this.SubscriptionArn = subscriptionArn;
        this.IsLocalPending = isLocalPending;
        this.Initiated = DateTime.Now;
      }

      private SnsAdapter ParentEndpoint { get; set; }
      public bool IsLocalPending { get; private set; }
      public DateTime Initiated { get; private set; }
      public string TopicArn { get; private set; }
      public string SubscriptionArn { get; private set; }

      public void Unsubscribe() {
        this.ParentEndpoint.UnSubscribe(this.SubscriptionArn);
      }

      public void Confirm(string token) {
        this.ParentEndpoint.Confirm(this.TopicArn, token);
      }

    }

  }

}
