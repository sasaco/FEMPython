using Assimp.Unmanaged;
using FarPoint.Excel;
using FrameWebforCS.components.input;
using FrameWebforCS.components.result;
using FrameWebforCS.providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using THREE;
using static FrameWebforCS.components.menu.SidebarComponent;

namespace FrameWebforCS
{
    internal class AppRoutingModule
    {
        // Lazy<T> を使ってスレッドセーフかつ遅延評価のシングルトンを実装
        private static readonly Lazy<AppRoutingModule> _instance =
            new Lazy<AppRoutingModule>(() => new AppRoutingModule());

        // 外部からはこのプロパティを通じてのみインスタンスにアクセスできる
        public static AppRoutingModule Instance => _instance.Value;


        private InputDataService _input = InputDataService.Instance;
        // フォームを最前面に常時表示する設定
        Form? floatForm = null;

        public List<UserControl> myComponents = new List<UserControl>();

        internal string? ActiveModeKey { get; private set; }
        internal event Action<string>? InputModeChanged;

        // コンストラクタを private にして、外部からの new を禁止する
        private AppRoutingModule()
        {
            _input.DimensionChanged += OnDimensionChanged;
        }

        internal void PrepareDimensionChange()
        {
            foreach (var component in myComponents)
            {
                if (component.IsDisposed) continue;
                foreach (var spread in component.Controls.OfType<components.myFpSpread>())
                    if (spread.EditMode) spread.StopCellEditing();
            }
        }

        private void OnDimensionChanged(int _)
        {
            if (_input.dimension == 2 && _input.CurrentComponent is InputPanelComponent &&
                floatForm is { IsDisposed: false })
            {
                floatForm.Close();
                _input.CurrentComponent = null;
            }

            foreach (var component in myComponents.ToArray())
            {
                if (component.IsDisposed) continue;
                switch (component)
                {
                    case InputNodesComponent nodes: nodes.RefreshDimension(); break;
                    case InputMembersComponent members: members.RefreshDimension(); break;
                    case InputElementsComponent elements: elements.RefreshDimension(); break;
                    case InputFixNodeComponent fixNode: fixNode.RefreshDimension(); break;
                    case InputFixMemberComponent fixMember: fixMember.RefreshDimension(); break;
                    case InputJointComponent joint: joint.RefreshDimension(); break;
                    case InputLoadComponent load: load.RefreshDimension(); break;
                }
            }

            if (floatForm is { IsDisposed: false } && _input.CurrentComponent is { IsDisposed: false } active)
                floatForm.Width = active.Width;
        }


        internal void contentsDailogShow(Type _target, string title, int option = -1, string? modeKey = null)
        {
            if (_target == null)
                return;

            var target = GetTargetComponent(_target);
            if (target == null)
                return;

            if (modeKey != null)
                // JS input components call ChangeMode("nodes"); C# sidebar keys use
                // singular "node". Notify even when the same component is reused.
                NotifyInputMode(modeKey);

            if (floatForm == null) {
                floatForm = new Form();
            }
            else if (floatForm.IsDisposed) {
                floatForm = new Form();
                _input.CurrentComponent = null;
            }

            // setActiveSheet 関数がある場合は実行する
            var info = target.GetType().GetMethod("setActiveSheet");
            if (info != null)
                info.Invoke(target, new object[] { option });

            // 表示している画面と同じなら再表示しない
            if (_input.CurrentComponent != null) {
                if (_input.CurrentComponent.Equals(target))
                    return;
            }

            // 非モーダルで表示
            floatForm.Text = title;
            floatForm.Controls.Clear();
            floatForm.Controls.Add(target);

            // サイズをコントロールの幅に合わせる
            Form? mainForm = Application.OpenForms.Count > 0 ? Application.OpenForms[0] : null;
            if (mainForm != null) {
                floatForm.Owner = mainForm;
                floatForm.Height = (int)(mainForm.Height * 0.6);
            }
            floatForm.Width = target.Width;
            target.Dock = DockStyle.Fill;

            floatForm.Show();

            // 記憶
            _input.CurrentComponent = target;

        }

        internal void NotifyInputMode(string modeKey)
        {
            ActiveModeKey = modeKey;
            InputModeChanged?.Invoke(modeKey);
        }

        /// <summary>
        /// Componentを取得する（なければ作成する）
        /// </summary>
        private UserControl? GetTargetComponent(Type _target)
        {
            var result = this.myComponents.Find(x => x.GetType() == _target);
            if (result == null)
            {
                result = createComponent(_target);
            } 
            else if (result.IsDisposed)
            {
                this.myComponents.Remove(result);
                result = createComponent(_target);
            }
            return result;

        }

        /// <summary>
        /// Componentを作成してリストに登録する
        /// </summary>
        /// <param name="_target"></param>
        /// <returns></returns>
        private UserControl? createComponent(Type _target)
        {
            var result = (UserControl?)Activator.CreateInstance(_target);
            if (result != null)
            {
                this.myComponents.Add(result);
            }
            return result;
        }
    }
}
