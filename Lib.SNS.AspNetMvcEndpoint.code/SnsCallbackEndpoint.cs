using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Amazon.SimpleNotificationService {

  public abstract class SnsCallbackEndpoint : ControllerBase,
    IDisposable {

    private readonly SnsAdapter _SnsAdapter;
    private readonly ILogger _Logger;
    private readonly LogLevel _LogAllIncommingCalls;
    private readonly Dictionary<string, Action> _CustomActions = new Dictionary<string, Action>();

    public SnsCallbackEndpoint(SnsAdapter snsAdapter, ILogger logger, LogLevel logAllIncommingCalls) {
      _SnsAdapter = snsAdapter;
      _Logger = logger;
      _LogAllIncommingCalls = logAllIncommingCalls;
      _SnsAdapter.BindObject(this);
    }

    protected void RegisterCustomAction(string name, Action handler) {
      _CustomActions[name] = handler;
    }

    public partial class MessageTypeProbe {
      public string Type { get; set; }

    }

    public partial class AwsSubscriptionConfirmationRequest {
      public string TopicArn { get; set; }
      public string Token { get; set; }
    }

    public partial class AwsNotification {
      public string Message { get; set; }
      public string MessageId { get; set; }
      public string Subject { get; set; } = null;
      public string Timestamp { get; set; }
      public string TopicArn { get; set; }
      public string Type { get; set; }
     
      public string SignatureVersion { get; set; }
      public string Signature { get; set; }
      public string SigningCertURL { get; set; }

      public byte[] GetDecodedSignature() {
        return Convert.FromBase64String(this.Signature);
      }

      private static Dictionary<string, X509Certificate2> _CertificatesByUrl = new Dictionary<string, X509Certificate2>();

      public X509Certificate2 GetCertificate() {
        lock (_CertificatesByUrl) {
          if (_CertificatesByUrl.ContainsKey(this.SigningCertURL)) {
            return _CertificatesByUrl[this.SigningCertURL];
          }
          else {
            using (WebClient wc = new WebClient()) {
              byte[] rawCert = wc.DownloadData(this.SigningCertURL);
              X509Certificate2 cert = new X509Certificate2(rawCert);
              _CertificatesByUrl.Add(this.SigningCertURL, cert);
              return cert;
            }
          }
        }
      }

      // https://docs.aws.amazon.com/sns/latest/dg/sns-verify-signature-of-message.html
      // https://github.com/boto/boto3/issues/2508
      public string GetSignatureContent() {
        var sb = new StringBuilder(this.Message.Length + 1024);
        var unixLineBreak = System.Text.Encoding.ASCII.GetString(new byte[] { 10 });

        sb.Append("Message");
        sb.Append(unixLineBreak);
        sb.Append(this.Message);
        sb.Append(unixLineBreak);

        sb.Append("MessageId");
        sb.Append(unixLineBreak);
        sb.Append(this.MessageId);
        sb.Append(unixLineBreak);

        if (this.Subject != null) {
          sb.Append("Subject");
          sb.Append(unixLineBreak);
          sb.Append(this.Subject);
          sb.Append(unixLineBreak);
        }

        sb.Append("Timestamp");
        sb.Append(unixLineBreak);
        sb.Append(this.Timestamp);
        sb.Append(unixLineBreak);

        sb.Append("TopicArn");
        sb.Append(unixLineBreak);
        sb.Append(this.TopicArn);
        sb.Append(unixLineBreak);

        sb.Append("Type");
        sb.Append(unixLineBreak);
        sb.Append(this.Type);
        sb.Append(unixLineBreak);

        return sb.ToString();
      }

    }

    protected virtual void RawMessageHook(ref string rawMessage) {
    }
    protected virtual void ExceptionHook(Exception ex) {
    }

    /// <summary> SNS-RECEIVE (AWS calls this opartion for any delivered topic) </summary>
    [HttpPost(), Produces("text/plain"), Consumes("text/plain")]
    [SwaggerOperation(OperationId = "SNS-RECEIVE", Description = "SNS-RECEIVE (AWS calls this opartion for any delivered topic)")]
    public string ProcessIncommingRawMessage([FromBody] string rawMessage) {
      try {

        this.RawMessageHook(ref rawMessage);

        if (rawMessage == null || !rawMessage.StartsWith("{") || !rawMessage.EndsWith("}")) {
          return "FAILED - only json supported";
        }

        if (_LogAllIncommingCalls != LogLevel.None) {
          _Logger.Log(_LogAllIncommingCalls, "Incomming Message from AWS-SNS - Input: " + rawMessage);
        }

        var messageType = JsonConvert.DeserializeObject<MessageTypeProbe>(rawMessage).Type.ToLower();

        switch (messageType) {
          case "subscriptionconfirmation": // kommt von aws, wenn subscription beantragt wurde (zwecks bestätigung)
          {
              this.ConfirmSubscription(JsonConvert.DeserializeObject<AwsSubscriptionConfirmationRequest>(rawMessage));
              return "OK - confirming...";
            }

          case "notification": // kommt von aws als business-event!
          {
              var notificationMessage = JsonConvert.DeserializeObject<AwsNotification>(rawMessage);

              //this.ValidateSignature(
              //  notificationMessage.GetCertificate(),
              //  notificationMessage.GetSignatureContent(),
              //  notificationMessage.GetDecodedSignature()
              //);

              //https://docs.microsoft.com/de-de/dotnet/api/system.security.cryptography.x509certificates.x509certificate2.verify?view=net-6.0
              //https://github.com/boto/boto3/issues/2508
              //https://docs.aws.amazon.com/sns/latest/dg/sns-verify-signature-of-message.html
              //https://stackoverflow.com/questions/49079983/create-rsa-sha1-signature
              //http://sns-public-resources.s3.amazonaws.com/SNS_Message_Signing_Release_Note_Jan_25_2011.pdf

              var awsParsedMessage = Amazon.SimpleNotificationService.Util.Message.ParseMessage(rawMessage);
              if (!awsParsedMessage.IsMessageSignatureValid()) {
                _Logger.LogCritical("Exception while Processing incomming Message from AWS-SNS: INVALID SIGNATURE - Input: " + rawMessage);
                return "INVALID SIGNATURE";
              }

              this.ProcessNotification(notificationMessage);

              return "OK - queued...";
            }

          default: {
              return $"FAILED - unknown message type '{messageType}'";
            }
        }

      }
      catch (Exception ex) {
        this.ExceptionHook(ex);
        _Logger.LogCritical(ex, "Exception while Processing incomming Message from AWS-SNS: " + ex.Message + " - Input: " + rawMessage);
        return "FAILED - internal server error";
      }
    }

    //protected virtual bool ValidateSigningCert(X509Certificate2 cert) {
    //  if(!cert.Verify()){
    //    return false;
    //  }
    //  else {
    //    return cert.Subject.Contains("sns.amazonaws.com");
    //  }
    //}
    //    private void ValidateSignature(X509Certificate2 cert, string signatureContent, byte[] receivedSignature) {
    //      if (!this.ValidateSigningCert(cert)) {
    //        throw new Exception("Singing-Certificate validation failed!");
    //      }
    //      cert.GetPublicKey ...
    //    }

    /// <summary> Browser-Access (displays a diagnostics-page) </summary>
    [SwaggerOperation(OperationId = "GetOverviewPage", Description = "Browser-Access (displays a diagnostics-page)")]
    [HttpGet(), Produces("text/html")]
    public ContentResult GetOverviewPage([FromQuery] string m) {

      if (String.IsNullOrWhiteSpace(m)) {
        m = "list";
      }

      var sb = new StringBuilder();
      TextWriter html = new StringWriter(sb);
      html.WriteLine("<!doctype html>");
      html.WriteLine("<html><head><title>SNS-Callback-Endpoint</title></head><body style=\"font-family: Arial;font-size: 14px;\">");

      try {

        switch (m ?? "") {
          case "unsubscribe": {
              _SnsAdapter.UnSubscribeAll();
              System.Threading.Thread.Sleep(200);
              break;
            }

          case "resubscribe": {
              _SnsAdapter.ResubscribeBoundObjects();
              System.Threading.Thread.Sleep(200);
              break;
            }

          case "reload": // continue below
              {
              _SnsAdapter.ReloadExistingSubscriptions();
              break;
            }

          case "list": // continue below
              {
              break;
            }

          default: {
              if (_CustomActions.ContainsKey(m)) {
                _CustomActions[m].Invoke();
                System.Threading.Thread.Sleep(200);
              }
              else {
                html.WriteLine("<b>UNKNOWN METHOD!</b><hr>");
              }
              break;
            }
        }

      }
      catch (Exception ex) {
        html.WriteLine($"<span style=\"font-size: 12px; color: red\"><b>{ex.Message}</b><br><br>{ex.StackTrace}</span><hr>");
      }

      var cs = _SnsAdapter.Subscriptions.Select((sub)=> new { sub, conf=true});
      var ps = _SnsAdapter.LocalPendingSubscriptions.Select((sub) => new { sub, conf = false });

      html.WriteLine("Subscriptions for callback: " + _SnsAdapter.SubscriptionCallbackUrl + "<br>");


      html.WriteLine("<hr>");

      foreach (var s in cs.Union(ps).OrderBy((s)=>s.sub.TopicArn)) {
        html.WriteLine($"<span style=\"font-size: 12px;\"><b>TOPIC-ARN:</b>: {s.sub.TopicArn}");
        if (s.conf) {
          var suffixOfSubscriptionArn = s.sub.SubscriptionArn.Substring(s.sub.SubscriptionArn.LastIndexOf(':') + 1 );
          html.WriteLine($"</span><br><span style=\"font-size: 10px; color: green\"><b>CONFIRMED </b> ({suffixOfSubscriptionArn})</span><hr>");
        }
        else {
          html.WriteLine($"</span><br><span style=\"font-size: 10px; color: red\"><b>PENDING...</b></span><hr>");
        }
      }

      html.WriteLine("COUNT (confirmed): <b>" + cs.Count().ToString() + "</b><br><br>");

      html.Write("<a href=\"?m=reload\">Reload</a>&nbsp;| &nbsp;<a href=\"?m=unsubscribe\">Unsubscribe</a>&nbsp;| &nbsp;<a href=\"?m=resubscribe\">Resubscribe</a>");
      foreach (var kvp in _CustomActions) {
        html.Write($"&nbsp;|&nbsp;<a href=\"?m={kvp.Key}\">{kvp.Key}</a>");
      }
      html.WriteLine("<br><br>");
      html.WriteLine("</body></html>");

      // return sb.ToString();

      return new ContentResult {
        ContentType = "text/html",
        StatusCode = (int)HttpStatusCode.OK,
        Content = sb.ToString()
      };

    }

    private void ProcessNotification(AwsNotification notification) {
      Task.Run(() => {
        _SnsAdapter.Receive(notification.TopicArn, notification.Message);
      });
    }

    private void ConfirmSubscription(AwsSubscriptionConfirmationRequest message) {
      _SnsAdapter.Confirm(message.TopicArn, message.Token);
    }

    void IDisposable.Dispose() {
      _SnsAdapter.UnbindObject(this);
    }
  }

  //https://peterdaugaardrasmussen.com/2020/02/29/asp-net-core-how-to-make-a-controller-endpoint-that-accepts-text-plain/
  public class TextPlainInputFormatter : InputFormatter {
    private const string ContentType = "text/plain";

    public TextPlainInputFormatter() {
      this.SupportedMediaTypes.Add(ContentType);
    }

    public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context) {
      var request = context.HttpContext.Request;
      using (var reader = new StreamReader(request.Body)) {
        var content = await reader.ReadToEndAsync();
        return await InputFormatterResult.SuccessAsync(content);
      }
    }

    public override bool CanRead(InputFormatterContext context) {
      var contentType = context.HttpContext.Request.ContentType;
      return contentType.StartsWith(ContentType);
    }
  }

}