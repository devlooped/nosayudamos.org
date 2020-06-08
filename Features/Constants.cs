using System;

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
            public static DateTime DateOfBirth { get; } = new DateTime(2000, 3, 15);

            public static Person Create() =>
                new Person(Id, FirstName, LastName, PhoneNumber, Role.Donor, DateOfBirth);
        }

        public static class Donee
        {
            public const string Id = "22222222";
            public const string FirstName = "Marcel@";
            public const string LastName = nameof(Donee);
            public const string PhoneNumber = "222";
            public static DateTime DateOfBirth { get; } = new DateTime(2000, 3, 15);

            public static Person Create() =>
                new Person(Id, FirstName, LastName, PhoneNumber, Role.Donee, DateOfBirth);
        }

        public static class Donee2
        {
            public const string Id = "33333333";
            public const string FirstName = "Lucian@";
            public const string LastName = nameof(Donee2);
            public const string PhoneNumber = "333";
            public static DateTime DateOfBirth { get; } = new DateTime(2000, 3, 15);

            public static Person Create() =>
                new Person(Id, FirstName, LastName, PhoneNumber, Role.Donee, DateOfBirth);
        }

        public static class System
        {
            public const string PhoneNumber = "999";
        }
    }
}
