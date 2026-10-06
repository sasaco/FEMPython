using FastDeepCloner;
using FrameWebforCS.components.input;
using FrameWebforCS.components.result;
using FrameWebforCS.providers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace FrameWebforCS.components.menu
{


    public partial class SidebarComponent : UserControl
    {
        private AppRoutingModule routing = AppRoutingModule.Instance;
        private readonly InputDataService _input = InputDataService.Instance;
        
        // 2Dと3Dで表示が変わるNode
        private readonly TreeNode _panelNode;
        private readonly TreeNode _solidNode;
        private readonly TreeNode _bridgeNode;
        private readonly TreeNode _inputNode;

        internal class targetComponent
        {
            internal Type Component { get; set; } = null;
            internal int option { get; set; } = -1;
            internal string? title { get; set; } = null;
            internal string root { get; set; } = null;
            internal TreeNode TreeNode { get; set; } = new TreeNode();
        }

        private Dictionary<string, targetComponent> targetComponents = new Dictionary<string, targetComponent>
        {
            { "input", new targetComponent(){
                title = "入力"
            }},
            { "element", new targetComponent(){ 
                Component = typeof(InputElementsComponent),
                title = "材料",
                root = "input"
            }}, 
            { "node", new targetComponent(){ 
                Component = typeof(InputNodesComponent),
                title = "節点",
                root = "input"
            }}, 
            { "member",new targetComponent(){ 
                Component = typeof(InputMembersComponent),
                title = "部材",
                root = "input",
                option = 0  
            }},
            { "rigid", new targetComponent(){ 
                Component = typeof(InputMembersComponent),
                title = "剛域",
                root = "member",
                option = 1  
            }}, 
            { "notice_points", new targetComponent(){ 
                Component = typeof(InputNoticePointsComponent),
                title = "着目点",
                root = "member",
            }}, 
            { "shell", new targetComponent(){ 
                Component = typeof(InputPanelComponent),
                title = "パネル",
                root = "input"
            }},
            { "solid", new targetComponent(){
                title = "ソリッド",
                root = "input"
            }},
            { "fix_node", new targetComponent(){ 
                Component = typeof(InputFixNodeComponent),
                title = "支点",
                root = "input"
            }}, 
            { "fix_member", new targetComponent(){ 
                Component = typeof(InputFixMemberComponent),
                title = "バネ",
                root = "input"
            }},
            { "joint", new targetComponent(){ 
                Component = typeof(InputJointComponent),
                title = "結合",
                root = "input"
            }},
            { "load", new targetComponent(){ 
                Component = typeof(InputLoadComponent),
                title = "荷重",
                root = "input",
                option = 0  
            }},
            { "bridge_load", new targetComponent(){
                Component = typeof(InputBridgeLoadComponent),
                title = "橋面荷重",
                root = "input"
            }},
            { "Combine", new targetComponent(){ 
                Component = typeof(InputCombineComponent),
                title = "組合せ",
                root = "input",
                option = 0
            }} ,

            { "output", new targetComponent(){
                title = "出力"
            }},
            { "disg", new targetComponent(){ 
                Component = typeof(ResultDisgComponent),
                title = "変位",
                root = "output",
                option = 0 
            }},
            { "basedisg", new targetComponent(){
                Component = typeof(ResultDisgComponent),
                title = "基本Case",
                root = "disg",
                option = 0
            }},
            { "combdisg", new targetComponent(){ 
                Component = typeof(ResultCombineDisgComponent),
                title = "組合せ",
                root = "disg",
                option = 1  
            }}, 
            { "pickdisg", new targetComponent(){ 
                Component = typeof(ResultPickupDisgComponent),
                title = "ピックアップ",
                root = "disg",
                option = 2  
            }},
            { "reac", new targetComponent(){ 
                Component = typeof(ResultReacComponent),
                title = "反力",
                root = "output",
                option = 0  
            }},
            { "basereac", new targetComponent(){
                Component = typeof(ResultReacComponent),
                title = "基本Case",
                root = "reac",
                option = 0
            }},
            { "combreac", new targetComponent(){ 
                Component = typeof(ResultCombineReacComponent),
                title = "組合せ",
                root = "reac",
                option = 1  
            }}, 
            { "pickreac", new targetComponent(){ 
                Component = typeof(ResultPickupReacComponent),
                title = "ピックアップ",
                root = "reac",
                option = 2  
            }}, 
            { "fsec", new targetComponent(){ 
                Component = typeof(ResultFsecComponent),
                title = "断面力",
                root = "output",
                option = 0  
            }}, //断面力: 基本Case
            { "basefsec", new targetComponent(){
                Component = typeof(ResultFsecComponent),
                title = "基本Case",
                root = "fsec",
                option = 0
            }}, 
            { "combfsec", new targetComponent(){ 
                Component = typeof(ResultCombineFsecComponent),
                title = "組合せ",
                root = "fsec",
                option = 1  
            }}, 
            { "pickfsec", new targetComponent(){ 
                Component = typeof(ResultPickupFsecComponent),
                title = "ピックアップ",
                root = "fsec",
                option = 2  
            }}, 
        };

        public SidebarComponent()
        {
            InitializeComponent();
            treeView1.NodeMouseClick += treeView1_NodeMouseClick;
            setTreeView(targetComponents.Clone());
            _panelNode = treeView1.Nodes.Find("shell", true).Single();
            _solidNode = treeView1.Nodes.Find("solid", true).Single();
            _bridgeNode = treeView1.Nodes.Find("bridge_load", true).Single();
            _inputNode = treeView1.Nodes.Find("input", true).Single();
            treeView1.ExpandAll();
            _input.DimensionChanged += OnDimensionChanged;
            _input.FileReplaced += OnFileReplaced;
            Disposed += (_, _) =>
            {
                _input.DimensionChanged -= OnDimensionChanged;
                _input.FileReplaced -= OnFileReplaced;
            };
            RefreshDimensionEntries();
        }

        private void OnDimensionChanged(int _) => RefreshDimensionEntries();

        private void OnFileReplaced(long _) => RefreshDimensionEntries();

        private void RefreshDimensionEntries()
        {
            if (_input.dimension == 3)
            {
                if (_panelNode.Parent == null) _inputNode.Nodes.Insert(3, _panelNode);
                if (_solidNode.Parent == null) _inputNode.Nodes.Insert(4, _solidNode);
                if (_bridgeNode.Parent == null)
                {
                    int loadIndex = _inputNode.Nodes.IndexOfKey("load");
                    _inputNode.Nodes.Insert(loadIndex + 1, _bridgeNode);
                }
            }
            else
            {
                if (_panelNode.Parent != null) _panelNode.Remove();
                if (_solidNode.Parent != null) _solidNode.Remove();
                if (_bridgeNode.Parent != null) _bridgeNode.Remove();
            }
        }

        private void setTreeView(Dictionary<string, targetComponent> List)
        {
            if (List.Count == 0) return;

            var target = List.First();

            TreeNode treeNode1 = target.Value.TreeNode;
            treeNode1.Name = target.Key;
            treeNode1.Text = target.Value.title;

            // 自分の子要素を探す
            Dictionary<string, targetComponent> child = List
                .Where(item => item.Value.root == target.Key)
                .ToDictionary(item => item.Key, item => item.Value);
            // 自分の子要素を登録する
            foreach (var item in child)
            {
                treeNode1.Nodes.Add(item.Value.TreeNode);
            }

            // ルート要素を TreeView に登録する
            if (target.Value.root == null)
                this.treeView1.Nodes.Add(treeNode1);

            // 終了した要素を Listから削除する
            List.Remove(target.Key);

            // 再帰
            setTreeView(List);
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            // Mouse navigation is handled by NodeMouseClick, including repeated clicks.
            if (e.Action != TreeViewAction.ByMouse)
                ShowNode(e.Node);
        }

        private void treeView1_NodeMouseClick(object? sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                ShowNode(e.Node);
        }

        private void ShowNode(TreeNode? node)
        {
            var key = node?.Name;

            if (key == null)
                return;

            if (targetComponents.ContainsKey(key)) 
            {
                var value = targetComponents[key];
                if (value != null)
                {
                    // Pass the sidebar key because C# input components do not call
                    // JS ThreeService.ChangeMode when their view becomes active.
                    routing.contentsDailogShow(value.Component, value.title, value.option, key);

                    // TreeViewの見た目をチェック状態にする
                    node?.Checked = true;
                }
            }
            
        }


    }
}
