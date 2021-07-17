using Amazon.Runtime;
using Amazon.SimpleSystemsManagement.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Amazon.SimpleSystemsManagement {

  public partial class AwsParameterAdapter {

    public AwsParameterAdapter(string awsAccessKey, string awsSecretKey, string awsRegionName = null) {

      if (string.IsNullOrWhiteSpace(awsRegionName)) {
        this.AwsRegion = RegionEndpoint.EUCentral1;
      }
      else {
        this.AwsRegion = RegionEndpoint.EnumerableAllRegions.Where(r => String.Equals(r.SystemName, awsRegionName, StringComparison.InvariantCultureIgnoreCase)).Single();
      }

      this.AwsRegionName = this.AwsRegion.SystemName;
      this.AwsCredentials = new BasicAWSCredentials(awsAccessKey, awsSecretKey);
    }

    private AWSCredentials AwsCredentials { get; set; }
    private Dictionary<string, string> _ResolveCache = new Dictionary<string, string>();
    private Dictionary<object, ObjectBindingAdapter> _BoundObjects = new Dictionary<object, ObjectBindingAdapter>();

    public RegionEndpoint AwsRegion { get; private set; }
    public string AwsRegionName { get; private set; }
    public Dictionary<string, Func<string>> ParameterKeyPlaceholderResolvers { get; set; } = new Dictionary<string, Func<string>>();

    /// <summary>
    /// if the value of the given 'arnOrParameterKey' is already a arn, then the value will just be passed trough, otherwise
    /// it will be treated as parameterKey and the corresponding value from the parameterstore will be returned
    /// </summary>
    internal string ResolveToValidArn(string arnOrParameterKey) {
      if (arnOrParameterKey.StartsWith("arn:")) {
        return arnOrParameterKey;
      }
      else {
        return this.GetParamValue(arnOrParameterKey);
      }
    }

    public string GetParamValue(string paramKey) {

      lock (_ResolveCache) {

        string paramKeyWithoutPhs = paramKey;
        foreach (var phName in this.ParameterKeyPlaceholderResolvers.Keys) {
          string pt = "{" + phName + "}";
          if (paramKeyWithoutPhs.Contains(pt)) {
            string pv = this.ParameterKeyPlaceholderResolvers[phName].Invoke();
            paramKeyWithoutPhs = paramKeyWithoutPhs.Replace(pt, pv);
          }
        }

        if (_ResolveCache.ContainsKey(paramKeyWithoutPhs)) {
          return _ResolveCache[paramKeyWithoutPhs];
        }

        string pValue = null;
        using (var client = new AmazonSimpleSystemsManagementClient(this.AwsCredentials, this.AwsRegion)) {
          var request = new GetParameterRequest() { Name = paramKeyWithoutPhs };
          var t = client.GetParameterAsync(request);
          t.Wait();
          if (t.IsCompleted && t.Result is object && t.Result.Parameter is object) {
            _ResolveCache.Add(paramKeyWithoutPhs, t.Result.Parameter.Value);
            pValue = t.Result.Parameter.Value;
          }
        }

        if (pValue is object) {
          lock (_BoundObjects) {
            foreach (var adapter in _BoundObjects.Values) {
              adapter.InjectValue(paramKey, pValue);
              if (!((paramKeyWithoutPhs ?? "") == (paramKey ?? ""))) {
                adapter.InjectValue(paramKeyWithoutPhs, pValue);
              }
            }
          }
        }

        return pValue;
      }
    }

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

    private partial class ObjectBindingAdapter : IDisposable {

      private object _Target;
      private Type _TargetType;
      private AwsParameterAdapter _Endpoint;
    
      public ObjectBindingAdapter(object target, AwsParameterAdapter endpoint) {

        _Target = target;
        _TargetType = target.GetType();
        _Endpoint = endpoint;

        this.Wireup();
        this.ReInjectValues();

      }

      private void CrawlMembers(Action<PropertyInfo, string> pubEventVisitor) {
        foreach (var e in _TargetType.GetProperties()) {
          foreach (var a in e.GetCustomAttributes<InjectAwsParameterAttribute>())
            pubEventVisitor.Invoke(e, a.ParameterKey);
        }
      }

      private Dictionary<Action<string>, string> _Subscriptions = null;

      public void Wireup() {
        if (this.IsWiredUp) {
          throw new Exception("already wired up!");
        }

        _Subscriptions = new Dictionary<Action<string>, string>();
        this.CrawlMembers((prop, paramKey) => {
          Action<string> setterAction;
          switch (prop.PropertyType) {
            case var @case when @case == typeof(string): {
                setterAction = new Action<string>(value => prop.SetValue(_Target, value));
                break;
              }

            case var case1 when case1 == typeof(int): {
                setterAction = new Action<string>(value => {
                  int converted = 0;
                  int.TryParse(value, out converted);
                  prop.SetValue(_Target, (object)converted);
                });
                break;
              }

            case var case2 when case2 == typeof(DateTime): {
                setterAction = new Action<string>(value => {
                  var converted = new DateTime(1900, 1, 1);
                  DateTime.TryParse(value, out converted);
                  prop.SetValue(_Target, (object)converted);
                });
                break;
              }

            case var case3 when case3 == typeof(Guid): {
                setterAction = new Action<string>(value => {
                  var converted = Guid.Empty;
                  Guid.TryParse(value, out converted);
                  prop.SetValue(_Target, (object)converted);
                });
                break;
              }

            case var case4 when case4 == typeof(bool): {
                setterAction = new Action<string>(value => {
                  bool converted = false;
                  bool.TryParse(value, out converted);
                  prop.SetValue(_Target, (object)converted);
                });
                break;
              }

            default: {
                setterAction = new Action<string>(s => { });
                break;
              }
          }

          _Subscriptions.Add(setterAction, paramKey);
        });
      }

      public void InjectValue(string paramKey, string newValue) {
        if (_Subscriptions is object) {
          foreach (var kvp in _Subscriptions) {
            if ((kvp.Value ?? "") == (paramKey ?? "")) {
              kvp.Key.Invoke(newValue);
            }
          }
        }
      }

      public void ReInjectValues() {
        if (_Subscriptions is object) {
          foreach (var kvp in _Subscriptions) {
            string paramValue = _Endpoint.GetParamValue(kvp.Value);
            kvp.Key.Invoke(paramValue);
          }
        }
      }

      public bool IsWiredUp {
        get {
          return _Subscriptions is object;
        }
      }

      public void Dispose() {
        _Subscriptions = null;
      }
    }
  }

}
