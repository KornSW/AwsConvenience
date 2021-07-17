using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
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
      public string TopicArn { get; set; }
      public string Message { get; set; }
    }

    /// <summary> SNS-RECEIVE (AWS calls this opartion for any delivered topic) </summary>
    [HttpPost(), Produces("text/plain"), Consumes("text/plain")]
    [SwaggerOperation(OperationId = "SNS-RECEIVE", Description = "SNS-RECEIVE (AWS calls this opartion for any delivered topic)")]
    public string ProcessIncommingRawMessage([FromBody] string rawMessage) {
      try {

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
              this.ProcessNotification(JsonConvert.DeserializeObject<AwsNotification>(rawMessage));
              return "OK - queued...";
            }

          default: {
              return $"FAILED - unknown message type '{messageType}'";
            }
        }

      }
      catch (Exception ex) {
        _Logger.LogCritical(ex, "Exception while Processing incomming Message from AWS-SNS: " + ex.Message + " - Input: " + rawMessage);
        return "FAILED - internal server error";
      }
    }

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
      switch (m ?? "") {
        case "unsubscribe": {
            _SnsAdapter.UnSubscribeAll();
            System.Threading.Thread.Sleep(200);
            break;
          }

        case "resubscribe": {
            _SnsAdapter.ResubscribeBoundObjects();
            System.Threading.Thread.Sleep(1000);
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
              try {
                _CustomActions[m].Invoke();
              }
              catch (Exception ex) {
                html.WriteLine($"<b>{ex.Message}</b><hr>");
              }
              System.Threading.Thread.Sleep(200);
            }
            else {
              html.WriteLine("<b>UNKNOWN METHOD!</b><hr>");
            }
            break;
          }
      }

      html.WriteLine("Subscriptions for callback: " + _SnsAdapter.SubscriptionCallbackUrl + "<br>");
      html.WriteLine("<hr>");
      foreach (var s in _SnsAdapter.Subscriptions) {
        html.WriteLine($"<span style=\"font-size: 10px;\"><b>SUBSCRIPTION-ARN:</b> {s.SubscriptionArn}<br><b>TOPIC-ARN:</b>: {s.TopicArn}</span><hr>");
      }
      html.WriteLine("COUNT: <b>" + _SnsAdapter.Subscriptions.Length.ToString() + "</b><br><br>");
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
      Task.Run(() => _SnsAdapter.Receive(notification.TopicArn, notification.Message));
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