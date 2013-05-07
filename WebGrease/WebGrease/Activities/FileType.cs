namespace WebGrease.Activities
{
    using System;

    /// <summary>
    /// private enumeration for the type of files being worked on
    /// </summary>
    [Flags]
    public enum FileTypes
    {
        None = 0,
        Image = 1,
        JavaScript = 2,
        StyleSheet = 4,
        All = 7
    }
}