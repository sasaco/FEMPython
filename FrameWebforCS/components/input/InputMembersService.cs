using FrameWebforCS.providers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Text.Json;

namespace FrameWebforCS.components.input
{
    internal readonly record struct DisplayMember(int Ni, int Nj, int Element, float Cg);

    internal class clsMember : INotifyPropertyChanged
    {
        public string? ni = null;
        public string? nj = null;
        public string? e = null;
        public float? cg = null;

        public event PropertyChangedEventHandler? PropertyChanged;

        public string? Ni { get => ni; set { ni = value; Changed(nameof(Ni)); } }
        public string? Nj { get => nj; set { nj = value; Changed(nameof(Nj)); } }
        public string? E { get => e; set { e = value; Changed(nameof(E)); } }
        public float? Cg { get => cg; set { cg = value; Changed(nameof(Cg)); } }

        public bool IsEmpty => string.IsNullOrWhiteSpace(ni) && string.IsNullOrWhiteSpace(nj)
            && string.IsNullOrWhiteSpace(e) && cg == null;

        private void Changed(string name) => DocumentReplacementNotifications.Publish(PropertyChanged, this, new PropertyChangedEventArgs(name));
    }

    internal class InputMembersService
    {
        private const int MaxNodeId = 100_000;
        private static readonly Lazy<InputMembersService> _instance = new(() => new InputMembersService());
        public static InputMembersService Instance => _instance.Value;

        private Dictionary<string, clsMember> _member = new();
        public BindingList<clsMember> Members { get; } = new();
        internal event Action<int>? MemberEdited;

        private InputMembersService()
        {
            Members.AllowNew = false;
            Members.AllowRemove = false;
            Members.RaiseListChangedEvents = false;
            for (int row = 0; row < MaxNodeId; row++)
                Members.Add(new clsMember());
            Members.RaiseListChangedEvents = true;
            Members.ListChanged += Members_ListChanged;
        }

        public void clear() => ReplaceRows(new Dictionary<string, clsMember>());

        public void setMemberJson(JsonElement jsonData)
        {
            ApplyMembers(ParseMemberJson(jsonData));
        }

        internal static Dictionary<string, clsMember> ParseMemberJson(JsonElement jsonData)
        {
            if (jsonData.ValueKind != JsonValueKind.Object)
                throw new JsonException("Input data must be an object.");
            if (!jsonData.TryGetProperty("member", out JsonElement memberJson))
                return new Dictionary<string, clsMember>();
            if (memberJson.ValueKind != JsonValueKind.Object)
                throw new JsonException("member must be an object.");

            var next = new Dictionary<string, clsMember>();
            var seenIds = new HashSet<string>();
            foreach (JsonProperty entry in memberJson.EnumerateObject())
            {
                if (!int.TryParse(entry.Name, NumberStyles.None, CultureInfo.InvariantCulture, out int row) ||
                    row < 1 || row > MaxNodeId)
                    throw new JsonException($"Invalid member: {entry.Name}");
                string id = row.ToString(CultureInfo.InvariantCulture);
                if (!seenIds.Add(id))
                    throw new JsonException($"Duplicate member: {entry.Name}");
                if (entry.Value.ValueKind != JsonValueKind.Object)
                    throw new JsonException($"Invalid member: {entry.Name}");
                clsMember? member = DataHelperModule.JsonToClass<clsMember>(entry.Value);
                if (member != null && !member.IsEmpty)
                    next.Add(id, member);
            }
            return next;
        }

        internal void ApplyMembers(Dictionary<string, clsMember> members) => ReplaceRows(members);

        internal IReadOnlyDictionary<int, DisplayMember> GetDisplayMembers()
        {
            var result = new Dictionary<int, DisplayMember>();
            foreach (var (id, member) in _member)
                if (ToDisplayMember(member) is { } display)
                    result.Add(int.Parse(id, CultureInfo.InvariantCulture), display);
            return result;
        }

        internal DisplayMember? GetDisplayMember(int id) =>
            _member.TryGetValue(id.ToString(CultureInfo.InvariantCulture), out var member)
                ? ToDisplayMember(member) : null;

        private static DisplayMember? ToDisplayMember(clsMember member)
        {
            // JS getMemberJson(0) projects blank endpoints and cg to zero. A member
            // with no valid positive endpoint cannot produce geometry, so omit it here.
            if (!int.TryParse(member.Ni, NumberStyles.None, CultureInfo.InvariantCulture, out int ni) || ni <= 0 ||
                !int.TryParse(member.Nj, NumberStyles.None, CultureInfo.InvariantCulture, out int nj) || nj <= 0)
                return null;
            int.TryParse(member.E, NumberStyles.None, CultureInfo.InvariantCulture, out int element);
            return new DisplayMember(ni, nj, element, member.Cg ?? 0);
        }

        public Dictionary<string, object> getMemberJson()
        {
            var members = new Dictionary<string, object>();
            foreach (var (id, member) in _member)
                if (!member.IsEmpty)
                    members.Add(id, DataHelperModule.ClassToDictionary(member));
            return members;
        }

        private void Members_ListChanged(object? sender, ListChangedEventArgs e)
        {
            if (e.ListChangedType != ListChangedType.ItemChanged || e.NewIndex < 0)
                return;
            string id = (e.NewIndex + 1).ToString(CultureInfo.InvariantCulture);
            clsMember member = Members[e.NewIndex];
            if (member.IsEmpty)
                _member.Remove(id);
            else
                _member[id] = member;
            DocumentReplacementNotifications.Publish(MemberEdited, e.NewIndex + 1);
        }

        private void ReplaceRows(Dictionary<string, clsMember> next)
        {
            Members.RaiseListChangedEvents = false;
            try
            {
                foreach (string id in _member.Keys)
                    if (!next.ContainsKey(id))
                        Members[int.Parse(id, CultureInfo.InvariantCulture) - 1] = new clsMember();
                foreach (var (id, member) in next)
                    Members[int.Parse(id, CultureInfo.InvariantCulture) - 1] = member;
                _member = next;
            }
            finally
            {
                Members.RaiseListChangedEvents = true;
                DocumentReplacementNotifications.Defer(() => Members.ResetBindings());
            }
        }
    }
}
