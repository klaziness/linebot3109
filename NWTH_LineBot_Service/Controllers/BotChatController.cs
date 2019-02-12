using LineMessagingAPISDK;
using LineMessagingAPISDK.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace NWTH_LineBot_Service.Controllers
{
    public class BotChatController : ApiController
    {
        [RoutePrefix("webhook")]
        public class LineMessagesSampleController : ApiController
        {
            //private static readonly ILog Log4netLogger = LogManager.GetLogger(typeof(LineMessagesSampleController));
            //private Logger Logger = new Logger(Log4netLogger);
            private LineClient lineClient = new LineClient(ConfigurationManager.AppSettings["ChannelToken"].ToString());

            private async Task<bool> VaridateSignature(HttpRequestMessage request)
            {
                var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(ConfigurationManager.AppSettings["ChannelSecret"].ToString()));
                var computeHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(await request.Content.ReadAsStringAsync()));
                var contentHash = Convert.ToBase64String(computeHash);
                var headerHash = Request.Headers.GetValues("X-Line-Signature").First();
                return contentHash == headerHash;
            }

            [Route]
            public async Task<HttpResponseMessage> Post(HttpRequestMessage request)
            {
                if (!await VaridateSignature(request))
                    return request.CreateResponse(HttpStatusCode.BadRequest);

                var content = await request.Content.ReadAsStringAsync();
                Activity activity = JsonConvert.DeserializeObject<Activity>(content);
                foreach (Event LineEvent in activity.Events)
                {
                    Profile profile = await lineClient.GetProfile(LineEvent.Source.UserId);
                    switch (LineEvent.Type)
                    {
                        case EventType.Beacon:
                            //Logger.Info("Beacon event");
                            break;
                        case EventType.Follow:
                            //Logger.Info("Follow event");
                            break;
                        case EventType.Join:
                            //Logger.Info("Join event");
                            break;
                        case EventType.Leave:
                            //Logger.Info("Leave event");
                            break;
                        case EventType.Message:
                            //Logger.Info("Message event");
                            Message message = JsonConvert.DeserializeObject<Message>(LineEvent.Message.ToString());

                            switch (message.Type)
                            {
                                case MessageType.Text:
                                    var textMessageReply = await HandleTextMessage(LineEvent);
                                    await Reply(LineEvent, textMessageReply);
                                    break;
                                case MessageType.Audio:
                                case MessageType.Image:
                                case MessageType.Video:
                                    var mediaReplyMessage = await HandleMediaMessage(LineEvent);
                                    await Reply(LineEvent, mediaReplyMessage);
                                    break;
                                case MessageType.Sticker:
                                    var stickerReplyMessage = await HandleStickerMessage(LineEvent);
                                    await Reply(LineEvent, stickerReplyMessage);
                                    break;
                                case MessageType.Location:
                                    var locationReplyMessage = await HandleLocationMessage(LineEvent);
                                    await Reply(LineEvent, locationReplyMessage);
                                    break;
                            }
                            break;
                        case EventType.Postback:
                            //Logger.Info("Postback event");
                            //
                            var postbackReplyMessage = new TextMessage(LineEvent.Postback.Data);
                            await Reply(LineEvent, postbackReplyMessage);
                            break;
                        case EventType.Unfollow:
                            //Logger.Info("Unfollow event");
                            break;
                    }

                }
                return Request.CreateResponse(HttpStatusCode.OK, activity);
                //return Request.CreateResponse(HttpStatusCode.OK);
            }

            private async Task<Message> HandleTextMessage(Event lineEvent)
            {
                var textMessage = JsonConvert.DeserializeObject<TextMessage>(lineEvent.Message.ToString());
                Message replyMessage = null;
                if (textMessage.Text.ToLower() == "demo")
                {
                    List<TemplateAction> actions = new List<TemplateAction>();
                    actions.Add(new MessageTemplateAction("Buttons", "buttons"));
                    actions.Add(new MessageTemplateAction("Confirm", "confirm"));
                    actions.Add(new MessageTemplateAction("Carousel", "carousel"));
                    actions.Add(new MessageTemplateAction("Imagemap", "imagemap"));
                    ButtonsTemplate buttonsTemplate = new ButtonsTemplate(null, "可用功能", "直接選比較快", actions);

                    replyMessage = new TemplateMessage("Buttons", buttonsTemplate);
                }
                else if (textMessage.Text.ToLower() == "buttons")
                {
                    List<TemplateAction> actions = new List<TemplateAction>();
                    actions.Add(new MessageTemplateAction("Message Label", "sample data"));
                    actions.Add(new PostbackTemplateAction("Postback Label", "postback data", null));
                    actions.Add(new UriTemplateAction("Uri Label", "https://github.com/kenakamu"));
                    ButtonsTemplate buttonsTemplate = new ButtonsTemplate("https://github.com/apple-touch-icon.png", "Sample Title", "Sample Text", actions);

                    replyMessage = new TemplateMessage("Buttons", buttonsTemplate);
                }
                else if (textMessage.Text.ToLower() == "confirm")
                {
                    List<TemplateAction> actions = new List<TemplateAction>();
                    actions.Add(new MessageTemplateAction("OK", "ok"));
                    actions.Add(new MessageTemplateAction("Cancel", "cancel"));
                    ConfirmTemplate confirmTemplate = new ConfirmTemplate("Are you sure?", actions);
                    replyMessage = new TemplateMessage("Confirm", confirmTemplate);
                }
                else if (textMessage.Text.ToLower() == "carousel")
                {
                    List<TemplateColumn> columns = new List<TemplateColumn>();
                    List<TemplateAction> actions = new List<TemplateAction>();
                    actions.Add(new MessageTemplateAction("Message Label", "sample data"));
                    actions.Add(new PostbackTemplateAction("Postback Label", "postback data", "postback text"));
                    actions.Add(new UriTemplateAction("Uri Label", "https://github.com/kenakamu"));
                    columns.Add(new TemplateColumn() { Title = "Casousel 1 Title", Text = "Casousel 1 Text", ThumbnailImageUrl = "https://github.com/apple-touch-icon.png", Actions = actions });
                    columns.Add(new TemplateColumn() { Title = "Casousel 2 Title", Text = "Casousel 2 Text", ThumbnailImageUrl = "https://github.com/apple-touch-icon.png", Actions = actions });
                    CarouselTemplate carouselTemplate = new CarouselTemplate(columns);

                    replyMessage = new TemplateMessage("Carousel", carouselTemplate);
                }
                else if (textMessage.Text.ToLower() == "imagemap")
                {
                    var url = HttpContext.Current.Request.Url;
                    var imageUrl = $"{url.Scheme}://{url.Host}:{url.Port}/images/githubicon";
                    List<ImageMapAction> actions = new List<ImageMapAction>();
                    actions.Add(new UriImageMapAction("http://github.com", new ImageMapArea(0, 0, 520, 1040)));
                    actions.Add(new MessageImageMapAction("I love LINE!", new ImageMapArea(520, 0, 520, 1040)));
                    replyMessage = new ImageMapMessage(imageUrl, "GitHub", new BaseSize(1040, 1040), actions);
                }
                else
                {
                    replyMessage = new TextMessage(textMessage.Text);
                }
                return await Task.FromResult(replyMessage);
            }

            private async Task<Message> HandleMediaMessage(Event lineEvent)
            {
                Message message = JsonConvert.DeserializeObject<Message>(lineEvent.Message.ToString());
                Media media = await lineClient.GetContent(message.Id);
                Message replyMessage = new ImageMessage("https://github.com/apple-touch-icon.png", "https://github.com/apple-touch-icon.png");
                return await Task.FromResult(replyMessage);
            }

            private async Task<Message> HandleStickerMessage(Event lineEvent)
            {
                var stickerMessage = JsonConvert.DeserializeObject<StickerMessage>(lineEvent.Message.ToString());
                return await Task.FromResult<Message>(new StickerMessage(" 1 ", " 1 "));
            }

            private async Task<Message> HandleLocationMessage(Event lineEvent)
            {
                var locationMessage = JsonConvert.DeserializeObject<LocationMessage>(lineEvent.Message.ToString());
                var replyMessage = new LocationMessage(
                    locationMessage.Title,
                    locationMessage.Address,
                    locationMessage.Latitude,
                    locationMessage.Longitude);
                return await Task.FromResult(replyMessage);
            }

            private async Task Reply(Event lineEvent, Message replyMessage)
            {
                try
                {
                    await lineClient.ReplyToActivityAsync(lineEvent.CreateReply(message: replyMessage));
                }
                catch
                {
                    await lineClient.PushAsync(lineEvent.CreatePush(message: replyMessage));
                }
            }
        }
    }
}
