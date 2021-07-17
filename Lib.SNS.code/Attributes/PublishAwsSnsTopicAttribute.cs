using System;

namespace Amazon.SimpleNotificationService {

  [AttributeUsage(AttributeTargets.Event, Inherited = true, AllowMultiple = true)]
  public partial class PublishAwsSnsTopicAttribute : Attribute {

    public PublishAwsSnsTopicAttribute(string topicArnOrParamName) {
      this.TopicArnOrParamName = topicArnOrParamName;
    }

    public string TopicArnOrParamName { get; private set; }

  }

}
