using FrameWebforCS.providers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text.Json;

namespace FrameWebforCS.components.input
{
    internal class clsRigit : INotifyPropertyChanged
    {
        private float? _iLength;
        private float? _jLength;
        private int? _rigidMaterial;

        public clsRigit(int row) => Row = row;

        public int Row { get; }
        public string? E => InputMembersService.Instance.Members[Row - 1].E;
        public float? Ilength { get => _iLength; set { _iLength = value; Changed(nameof(Ilength)); } }
        public float? Jlength { get => _jLength; set { _jLength = value; Changed(nameof(Jlength)); } }
        public int? E1 { get => _rigidMaterial; set { _rigidMaterial = value; Changed(nameof(E1)); } }
        public bool IsEmpty => Ilength == null && Jlength == null && E1 == null;

        public event PropertyChangedEventHandler? PropertyChanged;

        public void NotifyMemberChanged() => Changed(nameof(E));

        private void Changed(string name) => DocumentReplacementNotifications.Publish(PropertyChanged, this, new PropertyChangedEventArgs(name));
    }

    internal class InputRigidZoneService
    {
        private const int MaxMemberId = 100_000;
        private static readonly Lazy<InputRigidZoneService> _instance = new(() => new InputRigidZoneService());
        public static InputRigidZoneService Instance => _instance.Value;

        private HashSet<int> _activeRows = new();
        public BindingList<clsRigit> Rows { get; } = new();
        internal event EventHandler? Changed;

        internal IReadOnlyList<(int Row, float ILength, float JLength)> GetDisplaySnapshot() =>
            _activeRows.OrderBy(row => row).Select(row =>
                (row, Rows[row - 1].Ilength ?? 0, Rows[row - 1].Jlength ?? 0)).ToArray();

        private InputRigidZoneService()
        {
            Rows.AllowNew = false;
            Rows.AllowRemove = false;
            Rows.RaiseListChangedEvents = false;
            for (int row = 1; row <= MaxMemberId; row++)
                Rows.Add(new clsRigit(row));
            Rows.RaiseListChangedEvents = true;
            Rows.ListChanged += Rows_ListChanged;
            InputMembersService.Instance.Members.ListChanged += Members_ListChanged;
        }

        public void clear() => ReplaceRows(new Dictionary<int, (float? ILength, float? JLength, int? Material)>());

        public void setRigidJson(JsonElement jsonData)
        {
            ApplyRigid(ParseRigidJson(jsonData));
        }

        internal static Dictionary<int, (float? ILength, float? JLength, int? Material)> ParseRigidJson(JsonElement jsonData)
        {
            var next = new Dictionary<int, (float? ILength, float? JLength, int? Material)>();
            if (jsonData.TryGetProperty("rigid", out JsonElement rigidJson))
            {
                if (rigidJson.ValueKind != JsonValueKind.Array)
                    throw new JsonException("Invalid rigid data");
                foreach (JsonElement entry in rigidJson.EnumerateArray())
                {
                    if (entry.ValueKind != JsonValueKind.Object ||
                        !entry.TryGetProperty("m", out JsonElement idJson) ||
                        idJson.ValueKind != JsonValueKind.String ||
                        !int.TryParse(idJson.GetString(), NumberStyles.None, CultureInfo.InvariantCulture, out int row) ||
                        row < 1 || row > MaxMemberId || next.ContainsKey(row))
                        throw new JsonException("Invalid rigid member number");
                    next.Add(row, (
                        ReadRigidLength(entry, "Ilength"),
                        ReadRigidLength(entry, "Jlength"),
                        ReadRigidMaterial(entry)));
                }
            }
            return next;
        }

        internal void ApplyRigid(Dictionary<int, (float? ILength, float? JLength, int? Material)> prepared) =>
            ReplaceRows(prepared);

        public List<object> getRigidJson()
        {
            var result = new List<object>();
            foreach (int row in _activeRows.OrderBy(row => row))
            {
                clsRigit rigid = Rows[row - 1];
                if (rigid.E1 is int material)
                    result.Add(new {
                        m = row.ToString(CultureInfo.InvariantCulture),
                        Ilength = rigid.Ilength ?? 0,
                        Jlength = rigid.Jlength ?? 0,
                        e = material
                    });
            }
            return result;
        }

        private static float? ReadRigidLength(JsonElement entry, string name)
        {
            if (!entry.TryGetProperty(name, out JsonElement value) || value.ValueKind == JsonValueKind.Null)
                return null;
            if (value.ValueKind == JsonValueKind.Number && value.TryGetSingle(out float length) && float.IsFinite(length))
                return length;
            throw new JsonException($"Invalid rigid {name}");
        }

        private static int? ReadRigidMaterial(JsonElement entry)
        {
            if (!entry.TryGetProperty("e", out JsonElement value) || value.ValueKind == JsonValueKind.Null)
                return null;
            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int material))
                return material;
            throw new JsonException("Invalid rigid material");
        }

        private void ReplaceRows(Dictionary<int, (float? ILength, float? JLength, int? Material)> next)
        {
            Rows.RaiseListChangedEvents = false;
            try
            {
                foreach (int row in _activeRows)
                {
                    clsRigit rigid = Rows[row - 1];
                    rigid.Ilength = null;
                    rigid.Jlength = null;
                    rigid.E1 = null;
                }
                foreach (var (row, value) in next)
                {
                    clsRigit rigid = Rows[row - 1];
                    rigid.Ilength = value.ILength;
                    rigid.Jlength = value.JLength;
                    rigid.E1 = value.Material;
                }
                _activeRows = next.Where(item => item.Value.ILength != null ||
                    item.Value.JLength != null || item.Value.Material != null)
                    .Select(item => item.Key).ToHashSet();
            }
            finally
            {
                Rows.RaiseListChangedEvents = true;
                DocumentReplacementNotifications.Defer(() => Rows.ResetBindings());
            }
            DocumentReplacementNotifications.Publish(Changed, this, EventArgs.Empty);
        }

        private void Rows_ListChanged(object? sender, ListChangedEventArgs e)
        {
            if (e.ListChangedType != ListChangedType.ItemChanged || e.NewIndex < 0)
                return;
            int row = e.NewIndex + 1;
            if (Rows[e.NewIndex].IsEmpty)
                _activeRows.Remove(row);
            else
                _activeRows.Add(row);
            DocumentReplacementNotifications.Publish(Changed, this, EventArgs.Empty);
        }

        private void Members_ListChanged(object? sender, ListChangedEventArgs e)
        {
            if (e.ListChangedType == ListChangedType.ItemChanged && e.NewIndex >= 0)
                Rows[e.NewIndex].NotifyMemberChanged();
            else if (e.ListChangedType == ListChangedType.Reset)
                DocumentReplacementNotifications.Defer(() => Rows.ResetBindings());
        }
    }
}
