using System;

namespace MyNatsClient
{
    public class Credentials : IEquatable<Credentials>
    {
        public static readonly Credentials Empty = new Credentials();
        public string User { get; }
        public string Pass { get; }

        private Credentials() { }

        public Credentials(string user, string pass)
        {
            User = user;
            Pass = pass;
        }

        public static bool operator ==(Credentials left, Credentials right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Credentials left, Credentials right)
        {
            return !Equals(left, right);
        }

        public bool Equals(Credentials other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(User, other.User) && string.Equals(Pass, other.Pass);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Credentials);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (User.GetHashCode() * 397) ^ Pass.GetHashCode();
            }
        }
    }
}