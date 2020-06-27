using System;

namespace NosAyudamos
{
    static class Constants
    {
        public static class Donor
        {
            public const string Id = "10000000";
            public const string FirstName = "Fernand@";
            public const string LastName = nameof(Donor);
            public const string PhoneNumber = "1001001000";
            public static DateTime DateOfBirth { get; } = new DateTime(2000, 3, 15);

            public static global::NosAyudamos.Donor Create() =>
                new global::NosAyudamos.Donor(Id, FirstName, LastName, PhoneNumber, DateOfBirth);
        }

        public static class Donor2
        {
            public const string Id = "10000001";
            public const string FirstName = "Lucian@";
            public const string LastName = nameof(Donor);
            public const string PhoneNumber = "1011011001";
            public static DateTime DateOfBirth { get; } = new DateTime(2000, 3, 15);

            public static global::NosAyudamos.Donor Create() =>
                new global::NosAyudamos.Donor(Id, FirstName, LastName, PhoneNumber, DateOfBirth);
        }


        public static class Donee
        {
            public const string Id = "20000000";
            public const string FirstName = "Marcel@";
            public const string LastName = nameof(Donee);
            public const string PhoneNumber = "2002002000";
            public static DateTime DateOfBirth { get; } = new DateTime(2000, 3, 15);

            public static global::NosAyudamos.Donee Create() =>
                new global::NosAyudamos.Donee(Id, FirstName, LastName, PhoneNumber, DateOfBirth);
        }

        public static class Donee2
        {
            public const string Id = "20000002";
            public const string FirstName = "Marian@";
            public const string LastName = nameof(Donee2);
            public const string PhoneNumber = "2022022002";
            public static DateTime DateOfBirth { get; } = new DateTime(2000, 3, 15);

            public static global::NosAyudamos.Donee Create() =>
                new global::NosAyudamos.Donee(Id, FirstName, LastName, PhoneNumber, DateOfBirth);
        }

        public static class System
        {
            public const string PhoneNumber = "9099099999";
        }
    }
}
