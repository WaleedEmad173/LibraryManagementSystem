namespace LibraryManagementSystem.Models
{
    /// <summary>Shared circulation rules for workflows and their user interfaces.</summary>
    public static class CirculationPolicy
    {
        public const int MaxActiveLoans = 3;
        public const int LoanPeriodDays = 14;
    }

    /// <summary>Formats display identifiers from persisted database IDs.</summary>
    public static class LibraryIdentifiers
    {
        public static string Member(int id) => $"#MB-{id:D4}";
        public static string Book(int id) => $"#BOK-{id:D4}";
    }
}
