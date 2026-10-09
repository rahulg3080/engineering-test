using System;
using System.Collections.Generic; // REVIEW FIX: namespace was misspelled ("Collegctions"), which was a compile error.
using System.Linq;

namespace Utility.Valocity.ProfileHelper
{
    /// <summary>
    /// Comment: The original class was named "People" but represented a single person. Renamed to "Person" for clarity.
    /// Comment By:Rahul Gupta
    /// Comment Date: 2024-06-05
    /// A single person with a name and a date of birth.
    /// </summary>
    /// <remarks>
    /// REVIEW FIX: renamed from "People" to "Person" because an instance represents one person.
    /// </remarks>
    public class Person
    {
        /// <summary>The person's first name.</summary>
        public string Name { get; }

        /// <summary>The person's date of birth (UTC-based, with explicit offset).</summary>
        /// <remarks>REVIEW FIX: renamed from "DOB" to a full, readable name. Now get-only (immutable).</remarks>
        public DateTimeOffset DateOfBirth { get; }

        // REVIEW FIX: removed the single-argument constructor and the static "Under16" field.
        //  - The static field was evaluated once at type initialisation, so it went stale in   long-running processes.
        //  - Silently inventing a date of birth is a surprising default; callers must now be explicit.
        //  - Mixing DateTime (.Date => Kind "Unspecified") with DateTimeOffset caused implicit local-time conversions. We now use DateTimeOffset end to end.
        public Person(string name, DateTimeOffset dateOfBirth)
        {
            // REVIEW FIX: validate input instead of allowing a null/blank name into the object.
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            Name = name;
            DateOfBirth = dateOfBirth;
        }
    }

    /// <summary>
    /// Creates and queries a collection of randomly generated people.
    /// </summary>
    /// <remarks>
    /// REVIEW NOTE: "BirthingUnit" is a vague name; consider something like "PersonGenerator" or "PersonRegistry" depending on its real responsibility.
    /// </remarks>
    public class BirthingUnit
    {
        // REVIEW FIX: magic values moved into named constants.
        private const string MaleName = "Bob";
        private const string FemaleName = "Betty";
        private const int MinAge = 18;
        private const int MaxAgeExclusive = 85;   // Random.Next upper bound is exclusive => ages 18..84
        private const int OlderThanYears = 30;
        private const int MaxFullNameLength = 255;

        // REVIEW FIX: field is now readonly. Original XML doc ("MaxItemsToRetrieve") was
        // copy-pasted and wrong, so it has been replaced.
        private readonly List<Person> _people = new();

        // REVIEW FIX: dependencies injected so the class is unit-testable
        // (seeded Random and a fake TimeProvider make results deterministic).
        private readonly Random _random;
        private readonly TimeProvider _time;

        public BirthingUnit() : this(Random.Shared, TimeProvider.System) { }

        public BirthingUnit(Random random, TimeProvider timeProvider)
        {
            _random = random ?? throw new ArgumentNullException(nameof(random));
            _time = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        }

        /// <summary>
        /// Generates <paramref name="count"/> random people, stores them, and returns the newly created ones.
        /// </summary>
        /// <param name="count">Number of people to create. Must not be negative.</param>
        /// <returns>The people created by this call (not the whole internal collection).</returns>
        /// <remarks>
        /// REVIEW FIX (design): renamed from "GetPeople" because it mutates state (a "Get" should not).
        /// It also no longer returns the internal mutable list, so callers can't modify our state and repeated calls return only the new batch rather than everything accumulated so far.
        /// </remarks>
        public IReadOnlyList<Person> AddRandomPeople(int count)
        {
            // REVIEW FIX: a negative count used to silently return nothing; fail loudly instead.
            ArgumentOutOfRangeException.ThrowIfNegative(count);

            var created = new List<Person>(count);

            for (var n = 0; n < count; n++)
            {
                // REVIEW FIX: Next(0, 1) always returns 0 (upper bound is exclusive), so every person was "Bob". Next(0, 2) returns 0 or 1.
                var name = _random.Next(0, 2) == 0 ? MaleName : FemaleName;

                // REVIEW FIX: the original used "* 356" days (typo for 365, and ignores leap years).AddYears is exact. The "new Random()" per iteration was also removed (see constructor).
                var age = _random.Next(MinAge, MaxAgeExclusive);
                var dateOfBirth = _time.GetUtcNow().AddYears(-age);

                created.Add(new Person(name, dateOfBirth));
            }

            // REVIEW FIX: removed the try/catch. Nothing in the body is expected to fail, and the original catch threw a bare Exception that discarded the real error 
            // and stack trace (and left "e" unused). Let genuine exceptions propagate.
            _people.AddRange(created);
            return created;
        }

        /// <summary>
        /// Returns all people named "Bob", optionally only those older than 30.
        /// </summary>
        /// <param name="olderThan30">When true, only people born at least 30 years ago are returned.</param>
        /// <remarks>
        /// REVIEW FIX:
        ///  - Logic was inverted: "DOB >= now - 30y" selected people YOUNGER than 30. It is now "&lt;=".
        ///  - The cutoff is computed once instead of per element, and using UTC consistently (the original mixed UtcNow and Now).
        ///  - The Bob filter is no longer duplicated in both ternary branches.
        ///  - Returns a materialised snapshot (ToList) so a lazy query over a mutable list can't throw "collection was modified" later.
        ///  - Was private and never called (dead code); now public so it's part of the API and testable.
        /// </remarks>
        public IReadOnlyList<Person> GetBobs(bool olderThan30)
        {
            var bobs = _people.Where(p => p.Name == MaleName);

            if (olderThan30)
            {
                var cutoff = _time.GetUtcNow().AddYears(-OlderThanYears);
                bobs = bobs.Where(p => p.DateOfBirth <= cutoff);
            }
            return bobs.ToList();
        }

        /// <summary>
        /// Builds the person's full name using the given last name, capped at 255 characters.
        /// </summary>
        /// <remarks>
        /// REVIEW FIX:
        ///  - Renamed from "GetMarried" (reads like a mutator) to describe what it returns.
        ///  - The original length check was "(p.Name.Length + lastName).Length", which adds an int to a string ("3Smith"), so the length was meaningless. We measure the real full name.
        ///  - The original called Substring but discarded the result, so truncation never happened.
        ///  - Added null/blank guards (previously a NullReferenceException).
        ///  - REVIEW NOTE: the original had a special case "if lastName contains 'test' return the first name only". 
        /// </remarks>
        public string GetMarriedName(Person person, string lastName)
        {
            ArgumentNullException.ThrowIfNull(person);
            ArgumentException.ThrowIfNullOrWhiteSpace(lastName);

            var fullName = $"{person.Name} {lastName.Trim()}";

            return fullName.Length > MaxFullNameLength
                ? fullName[..MaxFullNameLength]
                : fullName;
        }
    }
}
