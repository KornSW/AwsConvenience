using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Amazon.SimpleNotificationService {

  partial class SnsAdapter {

    private partial class ObjectBindingAdapter : IDisposable {

      private object _Target;
      private Type _TargetType;
      private SnsAdapter _Endpoint;

      private List<IDisposable> _OutboundPipes = null;
      private List<IDisposable> _InboundPipes = null;

      private List<string> _ArnsToSubscribe = new List<string>();

      public ObjectBindingAdapter(object target, SnsAdapter endpoint, bool doWireUp = true) {

        _Target = target;
        _TargetType = target.GetType();
        _Endpoint = endpoint;

        if (doWireUp) {
          this.Wireup();
        }

      }

      private void CrawlMembers(
        Action<EventInfo, string> pubEventVisitor,
        Action<MethodInfo, string> subMethodVisitor
      ) {

        var exceptions = new List<Exception>();

        foreach (var e in _TargetType.GetEvents()) {
          foreach (var a in e.GetCustomAttributes<PublishAwsSnsTopicAttribute>()) {
            string arn = _Endpoint.ResolveToValidTopcArn(a.TopicArnOrParamName);
            try {
              pubEventVisitor.Invoke(e, arn);
            }
            catch (Exception ex) {
              exceptions.Add(ex);
            }
          }
        }

        foreach (var m in _TargetType.GetMethods()) {
          foreach (var a in m.GetCustomAttributes<SubscribeAwsSnsTopicAttribute>()) {
            string arn = _Endpoint.ResolveToValidTopcArn(a.TopicArnOrParamName);
            try {
              //this will call GetOrSubscribe
              subMethodVisitor.Invoke(m, arn);
            }
            catch (Exception ex) {
              exceptions.Add(ex);
            }
          }
        }

        if (exceptions.Any()) {
          throw new AggregateException(exceptions);
        }

      }

      public string[] ArnsToSubscribe {
        get {
          lock (_ArnsToSubscribe) {
            return _ArnsToSubscribe.ToArray();
          }
        }
      }

      public void Wireup() {
        if (this.IsWiredUp) {
          throw new Exception("already wired up!");
        }

        _OutboundPipes = new List<IDisposable>();
        _InboundPipes = new List<IDisposable>();

        lock (_ArnsToSubscribe) {
          _ArnsToSubscribe.Clear();

          this.CrawlMembers(
            (pubEvent, topicArn) => {
              var param = pubEvent.EventHandlerType.GetMethod("Invoke").GetParameters().Single();
              var genT = typeof(OutboundPipe<>).MakeGenericType(param.ParameterType);
              var args = new[] { _Endpoint, _Target, pubEvent, topicArn };
              _OutboundPipes.Add((IDisposable)Activator.CreateInstance(genT, args));
            },
            (subMethod, topicArn) => {
              _ArnsToSubscribe.Add(topicArn);
              var param = subMethod.GetParameters().Single();
              var genT = typeof(InboundPipe<>).MakeGenericType(param.ParameterType);
              var args = new[] { _Endpoint, _Target, subMethod, topicArn };
              _InboundPipes.Add((IDisposable)Activator.CreateInstance(genT, args));
            }
          );

        }
      }

      public bool IsWiredUp {
        get {
          return _OutboundPipes != null;
        }
      }

      public void Dispose() {

        foreach (var op in _OutboundPipes) {
          op.Dispose();
        }

        foreach (var ip in _InboundPipes) {
          ip.Dispose();
        }

        _OutboundPipes = null;
        _InboundPipes = null;
      }

      private partial class OutboundPipe<TMessage> : IDisposable {

        private SnsAdapter _Ep;
        private EventInfo _Evt;
        private object _Obj;
        private string _TopicArn;
        private Delegate _Handler;

        private static JsonSerializerSettings _JsonSerializerSettings = new JsonSerializerSettings {
          ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        public OutboundPipe(SnsAdapter ep, object obj, EventInfo evt, string topicArn) {
          _Ep = ep;
          _Evt = evt;
          _Obj = obj;
          _TopicArn = topicArn;
          var method = this.GetType().GetMethod(nameof(OnOutgoingMessage), BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.InvokeMethod);
          _Handler = Delegate.CreateDelegate(evt.EventHandlerType, this, method);
          _Evt.AddEventHandler(_Obj, _Handler);
        }

        private void OnOutgoingMessage(TMessage msg) {
          var rawMessage = JsonConvert.SerializeObject(msg, _JsonSerializerSettings);
          _Ep.PublishRawMessage(_TopicArn, rawMessage);
        }

        public void Dispose() {
          _Evt.RemoveEventHandler(_Obj, _Handler);
        }

      }

      private partial class InboundPipe<TMessage> : IDisposable {
        private SnsAdapter _Ep;
        private MethodInfo _Mth;
        private object _Obj;
        private string _TopicArn;

        public InboundPipe(SnsAdapter ep, object obj, MethodInfo mth, string topicArn) {
          _Ep = ep;
          _Mth = mth;
          _Obj = obj;
          _TopicArn = topicArn;
          _Ep.IncommingRawMessage += this.OnIncommingMessage;

          // trigger subscription
          _Ep.GetOrSubscribe(_TopicArn);
        }

        private void OnIncommingMessage(SubscriptionInfo ep, string rawMessage) {
          if ((ep.TopicArn ?? "") == (_TopicArn ?? "")) {
            var deserializedMessage = JsonConvert.DeserializeObject<TMessage>(rawMessage);
            _Mth.Invoke(_Obj, new object[] { deserializedMessage });
          }
        }

        public void Dispose() {
          _Ep.IncommingRawMessage -= this.OnIncommingMessage;
        }

      }
    }

  }

}
