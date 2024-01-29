namespace CLIApplication
{
    public static class TextReaderExtensions
    {
        public static async Task<string?> ForceReadLineAsync(this TextReader reader, CancellationToken cancellationToken = default)
        {
            Task<string?> readTask = Task.Run(() => reader.ReadLine());
            cancellationToken.ThrowIfCancellationRequested();
            await Task.WhenAny(readTask, Task.Delay(-1, cancellationToken));
            return await readTask;
        }
    }
}
