using System.Collections.Generic;
using TweetSharp;

namespace OneYearAgoTweets
{
	internal class TweetsComparer : IEqualityComparer<TwitterStatus>
	{
		public bool Equals(TwitterStatus x, TwitterStatus y)
		{
			return x.Id == y.Id;
		}

		public int GetHashCode(TwitterStatus obj)
		{
			return obj.Id.GetHashCode();
		}
	}
}