using SuperBat2.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using TweetSharp;

namespace SuperBat2
{
    class TwitterHelper
    {
        public static void SendTweet(string message)
        {
            try
            {
                message = DateTime.Now.ToString("T") + " " + message;
                var service = new TwitterService(Settings.Default.TwitterConsumerKey, Settings.Default.TwitterConsumerSecret);
                service.AuthenticateWith(Settings.Default.TwitterToken, Settings.Default.TwitterTokenSecret);
                var status = service.SendTweet(new SendTweetOptions { Status = message });

                if(service.Response.StatusCode == HttpStatusCode.OK)
                    Trace.TraceInformation("Tweet has been sent! \"{0}\"", message);
                else
                    Trace.TraceInformation("Tweet has not been sent! \"{0}\" Reason: {1}", message, service.Response.StatusDescription);
            }
            catch (Exception exception)
            {
                Trace.TraceWarning("Failed to send tweet! " + exception.Message);
            }
        }
    }
}
