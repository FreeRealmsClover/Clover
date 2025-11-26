namespace Sanctuary.Core.Helpers;

public static class JenkinsHelper
{
    public static uint OneAtATimeHash(string value)
    {
        var hash = 0;

        foreach (var valueChar in value)
        {
            hash += valueChar;
            hash += hash << 10;
            hash ^= hash >> 6;
        }

        hash += hash << 3;
        hash ^= hash >> 11;
        hash += hash << 15;

        return (uint)hash;
    }
}