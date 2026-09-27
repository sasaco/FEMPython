using FrameWebforCS.providers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json;

namespace FrameWebforCS.components.input
{
    internal class clsNoticePoints : INotifyPropertyChanged
    {
        public int row;
        public string? m = null;
        public List<float>? Points = null;
        private HashSet<int>? _enteredPoints;

        public event PropertyChangedEventHandler? PropertyChanged;
        public string? M { get => m; set { m = value; DocumentReplacementNotifications.Publish(PropertyChanged, this, new PropertyChangedEventArgs(nameof(M))); } }
        public float? P1 { get => PointAt(0); set => SetPoint(0, value, nameof(P1)); }
        public float? P2 { get => PointAt(1); set => SetPoint(1, value, nameof(P2)); }
        public float? P3 { get => PointAt(2); set => SetPoint(2, value, nameof(P3)); }
        public float? P4 { get => PointAt(3); set => SetPoint(3, value, nameof(P4)); }
        public float? P5 { get => PointAt(4); set => SetPoint(4, value, nameof(P5)); }
        public float? P6 { get => PointAt(5); set => SetPoint(5, value, nameof(P6)); }
        public float? P7 { get => PointAt(6); set => SetPoint(6, value, nameof(P7)); }
        public float? P8 { get => PointAt(7); set => SetPoint(7, value, nameof(P8)); }
        public float? P9 { get => PointAt(8); set => SetPoint(8, value, nameof(P9)); }
        public float? P10 { get => PointAt(9); set => SetPoint(9, value, nameof(P10)); }
        public float? P11 { get => PointAt(10); set => SetPoint(10, value, nameof(P11)); }
        public float? P12 { get => PointAt(11); set => SetPoint(11, value, nameof(P12)); }
        public float? P13 { get => PointAt(12); set => SetPoint(12, value, nameof(P13)); }
        public float? P14 { get => PointAt(13); set => SetPoint(13, value, nameof(P14)); }
        public float? P15 { get => PointAt(14); set => SetPoint(14, value, nameof(P15)); }
        public float? P16 { get => PointAt(15); set => SetPoint(15, value, nameof(P16)); }
        public float? P17 { get => PointAt(16); set => SetPoint(16, value, nameof(P17)); }
        public float? P18 { get => PointAt(17); set => SetPoint(17, value, nameof(P18)); }
        public float? P19 { get => PointAt(18); set => SetPoint(18, value, nameof(P19)); }
        public float? P20 { get => PointAt(19); set => SetPoint(19, value, nameof(P20)); }
        public bool IsEmpty => string.IsNullOrWhiteSpace(m) &&
            (Points == null || Points.All(point => point == 0));

        private float? PointAt(int index) =>
            Points != null && index < Points.Count &&
            (_enteredPoints == null || _enteredPoints.Contains(index)) ? Points[index] : null;

        private void SetPoint(int index, float? value, string name)
        {
            if (value.HasValue && !float.IsFinite(value.Value))
                throw new ArgumentOutOfRangeException(nameof(value));
            _enteredPoints ??= new HashSet<int>(Enumerable.Range(0, Points?.Count ?? 0));
            if (value.HasValue)
            {
                Points ??= new List<float>();
                while (Points.Count <= index) Points.Add(0);
                Points[index] = value.Value;
                _enteredPoints.Add(index);
            }
            else if (Points != null && index < Points.Count)
            {
                _enteredPoints.Remove(index);
                Points[index] = 0;
                while (Points.Count > 0 && !_enteredPoints.Contains(Points.Count - 1))
                    Points.RemoveAt(Points.Count - 1);
                if (Points.Count == 0) Points = null;
            }
            DocumentReplacementNotifications.Publish(PropertyChanged, this, new PropertyChangedEventArgs(name));
        }
    }

    internal class InputNoticePointsService
    {
        private const int MaxNodeId = 100_000;
        private static readonly Lazy<InputNoticePointsService> _instance = new(() => new InputNoticePointsService());
        public static InputNoticePointsService Instance => _instance.Value;

        private Dictionary<int, clsNoticePoints> _noticePoints = new();
        public BindingList<clsNoticePoints> NoticePoints { get; } = new();
        internal event EventHandler? Changed;

        internal IReadOnlyList<(int Row, string? Member, float[] Points)> GetDisplaySnapshot() =>
            _noticePoints.OrderBy(entry => entry.Key)
                .Select(entry => (entry.Key, entry.Value.m, entry.Value.Points?.ToArray() ?? Array.Empty<float>()))
                .ToArray();

        private InputNoticePointsService()
        {
            NoticePoints.AllowNew = false;
            NoticePoints.AllowRemove = false;
            NoticePoints.RaiseListChangedEvents = false;
            for (int index = 0; index < MaxNodeId; index++) NoticePoints.Add(new clsNoticePoints());
            NoticePoints.RaiseListChangedEvents = true;
            NoticePoints.ListChanged += RowsChanged;
        }

        public void clear() => ReplaceRows(new Dictionary<int, clsNoticePoints>());

        public void setNoticePointsJson(JsonElement jsonData)
        {
            var loaded = ParseNoticePointsJson(jsonData);
            if (loaded != null) ApplyNoticePoints(loaded);
        }

        internal static Dictionary<int, clsNoticePoints>? ParseNoticePointsJson(JsonElement jsonData)
        {
            if (!jsonData.TryGetProperty("notice_points", out JsonElement json)) return null;
            if (json.ValueKind != JsonValueKind.Array)
                throw new JsonException("notice_points must be an array.");
            foreach (var row in json.EnumerateArray())
            {
                if (row.ValueKind != JsonValueKind.Object)
                    throw new JsonException("notice_points row must be an object.");
                if (row.TryGetProperty("Points", out var points))
                {
                    if (points.ValueKind != JsonValueKind.Array)
                        throw new JsonException("notice_points Points must be an array.");
                    foreach (var point in points.EnumerateArray())
                        if (point.ValueKind != JsonValueKind.Number ||
                            !point.TryGetSingle(out float value) || !float.IsFinite(value))
                            throw new JsonException("Invalid notice_points point.");
                }
            }
            var loaded = DataHelperModule.JsonToList<clsNoticePoints>(json);
            if (loaded == null || loaded.Count != json.GetArrayLength())
                throw new JsonException("Invalid notice_points row.");
            var next = new Dictionary<int, clsNoticePoints>();
            foreach (var value in loaded)
            {
                if (value.row < 1 || value.row > MaxNodeId || !next.TryAdd(value.row, value))
                    throw new JsonException($"Invalid notice_points row: {value.row}");
            }
            return next;
        }

        internal void ApplyNoticePoints(Dictionary<int, clsNoticePoints>? prepared) =>
            ReplaceRows(prepared ?? new Dictionary<int, clsNoticePoints>());

        public List<Dictionary<string, object?>> getNoticePointsJson()
        {
            var result = new List<Dictionary<string, object?>>();
            foreach (var row in _noticePoints.Keys.OrderBy(row => row))
            {
                var value = _noticePoints[row];
                if (!value.IsEmpty) result.Add(DataHelperModule.ClassToDictionary(value));
            }
            return result;
        }

        private void RowsChanged(object? sender, ListChangedEventArgs e)
        {
            if (e.ListChangedType != ListChangedType.ItemChanged || e.NewIndex < 0) return;
            var value = NoticePoints[e.NewIndex];
            value.row = e.NewIndex + 1;
            if (value.IsEmpty) _noticePoints.Remove(value.row);
            else _noticePoints[value.row] = value;
            DocumentReplacementNotifications.Publish(Changed, this, EventArgs.Empty);
        }

        private void ReplaceRows(Dictionary<int, clsNoticePoints> next)
        {
            NoticePoints.RaiseListChangedEvents = false;
            try
            {
                foreach (var row in _noticePoints.Keys)
                    NoticePoints[row - 1] = new clsNoticePoints();
                foreach (var (row, value) in next)
                    NoticePoints[row - 1] = value;
                _noticePoints = next;
            }
            finally { NoticePoints.RaiseListChangedEvents = true; DocumentReplacementNotifications.Defer(() => NoticePoints.ResetBindings()); }
            DocumentReplacementNotifications.Publish(Changed, this, EventArgs.Empty);
        }
    }
}
