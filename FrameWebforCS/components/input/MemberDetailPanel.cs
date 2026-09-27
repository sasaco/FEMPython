using FrameWebforCS.providers;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace FrameWebforCS.components.input;

/// <summary>Shared member detail for both member and element routes.</summary>
internal sealed class MemberDetailPanel : Panel
{
    private readonly Dictionary<string, TextBox> _fields = new();
    private readonly Label _info;

    internal int? MemberId { get; private set; }

    internal MemberDetailPanel()
    {
        Name = "memberDetail";
        Dock = DockStyle.Right;
        Width = 255;
        AutoScroll = true;
        BackColor = SystemColors.ControlLightLight;
        Visible = false;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top, Width = 245, AutoSize = true,
            ColumnCount = 2, Padding = new Padding(8)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 86));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        var heading = new Label
        {
            Text = "部材詳細", AutoSize = true,
            Font = new Font(Font, FontStyle.Bold)
        };
        layout.Controls.Add(heading, 0, 0);
        layout.SetColumnSpan(heading, 2);
        AddField(layout, "i端", "ni");
        AddField(layout, "j端", "nj");
        AddField(layout, "材料No", "e");
        AddField(layout, "コード角(°)", "cg");
        _info = new Label { AutoSize = true, MaximumSize = new Size(220, 0) };
        int infoRow = layout.RowCount++;
        layout.Controls.Add(_info, 0, infoRow);
        layout.SetColumnSpan(_info, 2);
        AddField(layout, "弾性係数 E", "E", readOnly: true);
        AddField(layout, "せん断 G", "G", readOnly: true);
        AddField(layout, "熱膨張 Xp", "Xp", readOnly: true);
        AddField(layout, "断面積 A", "A", readOnly: true);
        AddField(layout, "ねじり J", "J", readOnly: true);
        AddField(layout, "断面二次 Iy", "Iy", readOnly: true);
        AddField(layout, "断面二次 Iz", "Iz", readOnly: true);
        AddField(layout, "材料名称 n", "n", readOnly: true);
        var save = new Button { Text = "更新", AutoSize = true };
        save.Click += (_, _) => SaveFields();
        int buttonsRow = layout.RowCount++;
        layout.Controls.Add(save, 0, buttonsRow);
        var close = new Button { Text = "閉じる", AutoSize = true };
        close.Click += (_, _) => Clear();
        layout.Controls.Add(close, 1, buttonsRow);
        Controls.Add(layout);
        InputDataService.Instance.FileReplaced += OnFileReplaced;
    }

    internal void ShowMember(int id)
    {
        if (IsDisposed || InputMembersService.Instance.GetDisplayMember(id) is not { } member)
        {
            Clear();
            return;
        }
        MemberId = id;
        _fields["ni"].Text = member.Ni.ToString(CultureInfo.InvariantCulture);
        _fields["nj"].Text = member.Nj.ToString(CultureInfo.InvariantCulture);
        _fields["e"].Text = member.Element.ToString(CultureInfo.InvariantCulture);
        _fields["cg"].Text = member.Cg.ToString(CultureInfo.InvariantCulture);
        UpdateMaterialFields(member.Element);
        UpdateDetailInfo(id, member);
        Visible = true;
        BringToFront();
    }

    internal bool ApplyMember(int ni, int nj, int element, float cg)
    {
        if (MemberId is not { } id || ni <= 0 || nj <= 0 || element < 0 ||
            !float.IsFinite(cg) || ni > 100_000 || nj > 100_000 || element > 100_000)
            return false;
        // One binding-list replacement publishes a complete row.
        InputMembersService.Instance.Members[id - 1] = new clsMember
        {
            Ni = ni.ToString(CultureInfo.InvariantCulture),
            Nj = nj.ToString(CultureInfo.InvariantCulture),
            E = element.ToString(CultureInfo.InvariantCulture),
            Cg = cg
        };
        UpdateMaterialFields(element);
        UpdateDetailInfo(id, new DisplayMember(ni, nj, element, cg));
        return true;
    }

    internal void Clear()
    {
        MemberId = null;
        Visible = false;
        foreach (var field in _fields.Values) field.Clear();
        _info.Text = "";
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) InputDataService.Instance.FileReplaced -= OnFileReplaced;
        base.Dispose(disposing);
    }

    private void OnFileReplaced(long revision) => Clear();

    private void AddField(TableLayoutPanel layout, string label, string key,
        bool readOnly = false)
    {
        int row = layout.RowCount++;
        layout.Controls.Add(new Label { Text = label, AutoSize = true,
            Anchor = AnchorStyles.Left }, 0, row);
        var field = new TextBox
        {
            Name = "memberDetail_" + key, Width = 125, ReadOnly = readOnly,
            BackColor = readOnly ? SystemColors.Control : SystemColors.Window
        };
        layout.Controls.Add(field, 1, row);
        _fields.Add(key, field);
    }

    private void UpdateMaterialFields(int elementId)
    {
        clsElement? material = null;
        var rows = InputElementsService.Instance.GetRows(1);
        if (elementId > 0 && elementId <= rows.Count)
        {
            var row = rows[elementId - 1];
            if (!row.IsEmpty) material = row;
        }
        static string Number(float? value) =>
            value?.ToString(CultureInfo.InvariantCulture) ?? "";
        _fields["E"].Text = Number(material?.ElasticModulus);
        _fields["G"].Text = Number(material?.ShearModulus);
        _fields["Xp"].Text = Number(material?.Expansion);
        _fields["A"].Text = Number(material?.Area);
        _fields["J"].Text = Number(material?.Torsion);
        _fields["Iy"].Text = Number(material?.InertiaY);
        _fields["Iz"].Text = Number(material?.InertiaZ);
        _fields["n"].Text = material?.Name ?? "";
    }

    private void SaveFields()
    {
        if (int.TryParse(_fields["ni"].Text, NumberStyles.None,
                CultureInfo.InvariantCulture, out int ni) &&
            int.TryParse(_fields["nj"].Text, NumberStyles.None,
                CultureInfo.InvariantCulture, out int nj) &&
            int.TryParse(_fields["e"].Text, NumberStyles.None,
                CultureInfo.InvariantCulture, out int element) &&
            float.TryParse(_fields["cg"].Text, NumberStyles.Float,
                CultureInfo.InvariantCulture, out float cg) &&
            ApplyMember(ni, nj, element, cg)) return;
        _info.Text = "有効な節点・材料番号と角度を入力してください。";
    }

    private void UpdateDetailInfo(int id, DisplayMember member)
    {
        var i = InputNodesService.Instance.GetDisplayNode(member.Ni);
        var j = InputNodesService.Instance.GetDisplayNode(member.Nj);
        if (i is not { } ni || j is not { } nj)
        {
            _info.Text = $"部材 {id}\n節点座標または長さは未定義です。";
            return;
        }
        float length = new THREE.Vector3().SubVectors(nj, ni).Length();
        _info.Text = string.Create(CultureInfo.InvariantCulture,
            $"部材 {id}\n長さ {length:0.###} m\ni端 ({ni.X:0.###}, {ni.Y:0.###}, {ni.Z:0.###})\nj端 ({nj.X:0.###}, {nj.Y:0.###}, {nj.Z:0.###})");
    }
}
