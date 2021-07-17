using System;

namespace Amazon.SimpleNotificationService {

  [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
  public partial class SubscribeAwsSnsTopicAttribute : Attribute {

    public SubscribeAwsSnsTopicAttribute(string topicArnOrParamName) {
      this.TopicArnOrParamName = topicArnOrParamName;
    }

    public string TopicArnOrParamName { get; private set; }

  }
}
