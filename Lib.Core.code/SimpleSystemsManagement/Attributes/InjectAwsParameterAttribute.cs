using System;

namespace Amazon.SimpleSystemsManagement {

  [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = true)]
  public partial class InjectAwsParameterAttribute : Attribute {

    public InjectAwsParameterAttribute(string parameterKey) {
      this.ParameterKey = parameterKey;
    }

    public string ParameterKey { get; private set; }

  }

}
