using Microsoft.Extensions.Logging;

namespace ClickOnceUninstaller;

public static class RetryHelpers
{
    public static T WithRetries<T>(
        this Func<T> func,
        ILogger logger,
        int max = 10
    )
    {
        Exception exception = null;
        var retries = 0;
        while (retries++ <= max)
        {
            try
            {
                return func();
            }
            catch (Exception e)
            {
                exception = e;
                var seconds = (int)Math.Pow(2, retries);
                logger.LogError(
                    e,
                    "Crash sleeping for {Seconds}. This was attempt number {Retries}",
                    seconds,
                    retries - 1
                );
                if (retries <= max)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(seconds));
                }
            }
        }

        throw new RetryFailedException(exception);
    }

    public static void WithRetries(
        this Action func,
        ILogger logger,
        int max = 10
    )
    {
        var retries = 0;
        Exception exception = null;
        while (retries++ <= max)
        {
            try
            {
                func();
                return;
            }
            catch (Exception e)
            {
                exception = e;
                var seconds = (int)Math.Pow(2, retries);
                logger.LogError(
                    e,
                    "Crash sleeping for {Seconds}. This was attempt number {Retries}",
                    seconds,
                    retries - 1
                );
                if (retries <= max)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(seconds));
                }
            }
        }
        throw new RetryFailedException(exception);
    }
}
public class RetryFailedException(Exception e) : Exception("Retry failed", e);