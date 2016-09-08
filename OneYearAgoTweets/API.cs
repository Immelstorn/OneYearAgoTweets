using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using TweetSharp;

namespace OneYearAgoTweets
{
    internal class API
    {
        #region vars

        public List<TwitterStatus> OldTweets = new List<TwitterStatus>();

        private static API _instance;
        private List<TwitterStatus> _newTweets = new List<TwitterStatus>();
        private int _remainingLimit;

        private static readonly object SyncLock = new object();
        private readonly TwitterService service;

        private const int Count = 200;
        private const string ScreenName = "";
        private const string ConsumerKey = "";
        private const string ConsumerSecret = "";
        private const string AccessToken = "";
        private const string AccessTokenSecret = "";

        #endregion

        private API()
        {
            service = new TwitterService(ConsumerKey, ConsumerSecret);
            service.AuthenticateWith(AccessToken, AccessTokenSecret);
        }

        public static API GetApi()
        {
            if (_instance == null)
            {
                lock (SyncLock)
                {
                    if (_instance == null)
                    {
                        _instance = new API();
                    }
                }
            }
            return _instance;
        }

        /// <summary>
        ///     Gets the old statuses.
        /// </summary>
        private void GetOldStatuses()
        {
            OldTweets = GetStatuses();
            Logs.WriteLog("log.txt", "OldTweets.Count=" + OldTweets.Count);
        }

        /// <summary>
        ///     Gets the statuses.
        /// </summary>
        /// <param name="statusId">The status id.</param>
        /// <returns></returns>
        private List<TwitterStatus> GetStatuses(long? statusId = 0)
        {
            List<TwitterStatus> tweetsResult = new List<TwitterStatus>();

            //получаем список всех твитов за последнее время
            Logs.WriteLog("log.txt", "получаем список всех твитов за последнее время");
            var options = new ListTweetsOnUserTimelineOptions
                              {ScreenName = ScreenName, Count = Count, ExcludeReplies = true, IncludeRts = false};

            if (statusId != null && statusId != 0)
            {
                options.SinceId = statusId;
            }

            int i = 1;
            while (true)
            {
                Logs.WriteLog("log.txt", "Страница № " + i);
                List<TwitterStatus> tweets = service.ListTweetsOnUserTimeline(options).ToList();
                Logs.WriteLog("log.txt", "tweets.Count = " + tweets.Count);

                if (!tweets.Any())
                    break;
                options.MaxId = tweets[tweets.Count - 1].Id - 1;

                tweetsResult.AddRange(
                    tweets.Where(twitterStatus => twitterStatus.CreatedDate.ToLocalTime() >= DateTime.Now.AddYears(-1)));
                i++;
            }
            Logs.WriteLog("log.txt", tweetsResult.Count.ToString());

            //убираем те, в которых есть реплаи и нейтрализуем хэштеги
            Logs.WriteLog("log.txt", "убираем те, в которых есть реплаи и нейтрализуем хэштеги");
            var tweets2 = new List<TwitterStatus>();
            foreach (TwitterStatus item in tweetsResult)
            {
                string text = item.Text;
                bool add = Reformat(ref text);
                item.Text = text;
                if (add)
                    tweets2.Add(item);
            }
            tweetsResult = tweets2;
            return tweetsResult;
        }

        private bool Reformat(ref string text)
        {
            if (string.IsNullOrEmpty(text) || text.StartsWith("@"))
            {
                return false;
            }

            if (text.Contains("@"))
            {
                text = text.Replace("@", "@  ");
            }

            if (text.Contains('#'))
            {
                text = text.Replace("#", "# ");
            }

            //заменяем приватбанк на английские буквы, ибо заебали звонить
            if (text.Contains("приватбанк"))
            {
                text = text.Replace("приватбанк", "пpивaтбaнk");
            }
            if (text.Contains("Приватбанк"))
            {
                text = text.Replace("Приватбанк", "Пpивaтбaнk");
            }
            if (text.Contains("приват"))
            {
                text = text.Replace("приват", "пpивaт");
            }
            return true;
        }

        /// <summary>
        ///     Gets the new statuses.
        /// </summary>
        /// <param name="statusId">The status id.</param>
        public void GetNewStatuses(long? statusId)
        {
            _newTweets = GetStatuses(statusId);

            Logs.WriteLog("log.txt", "NewTweets.Count=" + _newTweets.Count);
            Logs.WriteLog("log.txt", "Удаляем те что уже есть в БД");

            foreach (TwitterStatus item in _newTweets.Where(item => OldTweets.Contains(item, new TweetsComparer())))
            {
                _newTweets.Remove(item);
            }

            Logs.WriteLog("log.txt", "NewTweets.Count=" + _newTweets.Count);

            if (_newTweets.Count > 0)
            {
                Logs.WriteLog("log.txt", "добавляем новые твиты к старым");

                //добавляем новые твиты к старым
                foreach (TwitterStatus item in OldTweets)
                {
                    _newTweets.Add(item);
                }
                OldTweets = _newTweets;
            }

            Logs.WriteLog("log.txt", "OldTweets.Count=" + OldTweets.Count);
        }

        /// <summary>
        ///     Serializes this instance.
        /// </summary>
        public void Save()
        {
            Logs.WriteLog("log.txt", "Сохраняем в файл");
            var bf = new BinaryFormatter();
            using (var fs = new FileStream("oldTweets.dat", FileMode.Create))
            {
                bf.Serialize(fs, OldTweets);
            }
        }

        /// <summary>
        ///     Deserializes this instance.
        /// </summary>
        public void Load()
        {
            if (File.Exists("oldTweets.dat"))
            {
                Logs.WriteLog("log.txt", "Загружаем из файла");
                var bf = new BinaryFormatter();
                using (var fs = new FileStream("oldTweets.dat", FileMode.Open))
                {
                    OldTweets = (List<TwitterStatus>) bf.Deserialize(fs);
                }
                var temp = new List<TwitterStatus>();
                foreach (TwitterStatus item in OldTweets.Where(item => !temp.Contains(item, new TweetsComparer())))
                {
                    temp.Add(item);
                }
                OldTweets = temp;
                Logs.WriteLog("log.txt", "OldTweets = " + OldTweets.Count);
            }
            else
            {
                Logs.WriteLog("log.txt", "Загружаем из твиттера");
                GetOldStatuses();
            }
        }

        /// <summary>
        ///     Tweets the specified tweet.
        /// </summary>
        /// <param name="tweet">The tweet.</param>
        public void Tweet(string tweet)
        {
            if (!Reformat(ref tweet))
                return;

            //апдейтим статус переданный в аргументе
            if (tweet.Length > 140)
                tweet = tweet.Remove(140);

            Logs.WriteLog("log.txt", "апдейтим статус переданный в аргументе=> " + tweet);
            Limiter();
            var options = new SendTweetOptions {Status = tweet};
            service.SendTweet(options);
        }

        private void Limiter()
        {
            _remainingLimit = service.Response == null ? 1 : service.Response.RateLimitStatus.RemainingHits;

            while (_remainingLimit == 0)
            {
                Logs.WriteLog("log.txt",
                              "Rate limit exceeded, sleeping until " + service.Response.RateLimitStatus.ResetTime);
                Thread.Sleep(service.Response.RateLimitStatus.ResetTime - DateTime.Now);

                //проснулись. опять пощупали лимит, записали в переменную
                service.GetRateLimitStatus(new GetRateLimitStatusOptions());
                _remainingLimit = service.Response.RateLimitStatus.RemainingHits;
            }
            Logs.WriteLog("log.txt", "RateLimitStatus:" + _remainingLimit.ToString());
        }

        public void Converter()
        {
            Twitterizer.TwitterStatusCollection old = new Twitterizer.TwitterStatusCollection();

            var bf = new BinaryFormatter();
            using (var fs = new FileStream("oldTweets.dat", FileMode.Open))
            {
                old = (Twitterizer.TwitterStatusCollection) bf.Deserialize(fs);
            }
            var user = service.GetUserProfile(new GetUserProfileOptions());
            List<TwitterStatus> newT = old.Select(tweet => new TwitterStatus
                                                               {
                                                                   CreatedDate = tweet.CreatedDate,
                                                                   User = user,
                                                                   Id = long.Parse(tweet.Id.ToString()),
                                                                   InReplyToScreenName = tweet.InReplyToScreenName,
                                                                   InReplyToStatusId =
                                                                       tweet.InReplyToStatusId == null
                                                                           ? (long?) null
                                                                           : long.Parse(
                                                                               tweet.InReplyToStatusId.ToString()),
                                                                   InReplyToUserId =
                                                                       tweet.InReplyToUserId == null
                                                                           ? (int?) null
                                                                           : int.Parse(tweet.InReplyToUserId.ToString()),
                                                                   IsFavorited = tweet.IsFavorited ?? false,
                                                                   IsTruncated = tweet.IsTruncated ?? false,
                                                                   RetweetCount = tweet.RetweetCount ?? 0,
                                                                   Source = tweet.Source,
                                                                   Text = tweet.Text
                                                               }).ToList();

            using (var fs = new FileStream("oldTweetsNew.dat", FileMode.Create))
            {
                bf.Serialize(fs, newT);
            }
        }
    }
}