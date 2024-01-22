namespace CLIApplication
{
    public static class StringExtensions
    {
        /// <summary>
        /// Split a string by `by` argument, *except* when inside of `open` `close`.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="by"></param>
        /// <param name="open"></param>
        /// <param name="close"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">When string has opened `open` without closing</exception>
        public static List<string> DeliminateOutside(this string s, string by = " ", string open = "\"", string close = "\"", bool includeException = false, bool includeDelimiter = false)
        {
            s = s.Trim();
            List<string> deliminated = new();

            bool inException = false;

            deliminated.Add("");
            while (true)
            {
                if (s.StartsWith(close, StringComparison.InvariantCulture))
                {
                    inException = open == close && !inException;
                    if (!includeException)
                        s = s.Remove(0, close.Length);
                    if (s.Length == 0)
                        break;
                }
                else if (s.StartsWith(open, StringComparison.InvariantCulture))
                {
                    inException = true;
                    if (!includeException)
                        s = s.Remove(0, open.Length);
                }

                if (!inException && s.StartsWith(by, StringComparison.InvariantCulture))
                {
                    if (!includeDelimiter)
                        s = s.Remove(0, by.Length);
                    else
                        _ = deliminated.Last().Concat(by);
                    if (s.Length == 0)
                        break;
                    deliminated.Add("");
                    continue;
                }

                if (s.Length == 0)
                    break;
                deliminated[^1] += s[0];
                s = s.Remove(0, 1);
            }

            if (inException)
                throw new ArgumentException($"String {s} has opened {open} but not closed {close}");

            return deliminated;
        }
    }
}
