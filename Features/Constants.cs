namespace NosAyudamos
{
    static class Constants
    {
        public static class Donor
        {
            public const string Id = "11111111";
            public const string FirstName = "Fernand@";
            public const string LastName = nameof(Donor);
            public const string PhoneNumber = "111";

            public static Person Create() =>
                new Person(Id, FirstName, LastName, PhoneNumber);
        }

        public static class Donee
        {
            public const string Id = "22222222";
            public const string FirstName = "Marcel@";
            public const string LastName = nameof(Donee);
            public const string PhoneNumber = "222";

            public static Person Create() =>
                new Person(Id, FirstName, LastName, PhoneNumber);
        }

        public static class System
        {
            public const string PhoneNumber = "999";
        }
    }
}
