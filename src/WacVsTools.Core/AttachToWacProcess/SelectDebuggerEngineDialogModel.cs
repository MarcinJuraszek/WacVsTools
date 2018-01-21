namespace WacVsTools.Core.AttachToWacProcess
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;

    using Microsoft.Win32;

    public interface ISelectDebuggerEngineDialogModel
    {
        DebuggerEngines DebuggerEngines { get; set; }

        bool SelectionValid { get; }
    }

    public sealed class DebuggerEngines : INotifyPropertyChanged
    {
        private const string RegistryKeyPath = @"SOFTWARE\Microsoft\WAC\WacVsTools";
        private const string IsAutomaticValueName = @"IsAutomatic";
        private const string ManuallySelectedEnginesValueName = @"ManuallySelectedEngines";

        public const string DefaultDebuggerEngineName = @"Managed (v4.6, v4.5, v4.0) code";
        public const string DefaultDebuggerEngineID = @"{FB0D4648-F776-4980-95F8-BB7F36EBC1EE}";

        private static readonly DebuggerEngines s_default = new DebuggerEngines(isLazy: true, isAutomatic: true, manualSelection: new[] { new DebuggerEngine(DefaultDebuggerEngineName, DefaultDebuggerEngineID, isSelected: false) });

        private readonly bool m_isLazy;
        private bool m_isAutomatic;
        private readonly IReadOnlyList<DebuggerEngine> m_manualSelection;

        public DebuggerEngines(bool isAutomatic, IList<DebuggerEngine> manualSelection)
            : this(isLazy: false, isAutomatic: isAutomatic, manualSelection: manualSelection)
        {
        }

        private DebuggerEngines(bool isLazy, bool isAutomatic, IList<DebuggerEngine> manualSelection)
        {
            m_isLazy = isLazy;
            IsAutomatic = isAutomatic;
            m_manualSelection = manualSelection?.ToList() ?? throw new ArgumentNullException(nameof(ManualSelection));

            foreach (var debuggerEngine in m_manualSelection)
            {
                debuggerEngine.PropertyChanged += OnChildPropertyChanged;
            }
        }

        private void OnChildPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ManualSelection)));
        }

        public static DebuggerEngines DefaultLazy => s_default;

        public bool IsAutomatic
        {
            get
            {
                return m_isAutomatic;
            }
            set
            {
                if (m_isAutomatic != value)
                {
                    m_isAutomatic = value;

                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsAutomatic)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsManual)));
                }
            }
        }

        public void PersistSelectionToRegistry()
        {
            using (var registryKey = Registry.CurrentUser.CreateSubKey(RegistryKeyPath))
            {
                registryKey.SetValue(IsAutomaticValueName, IsAutomatic, RegistryValueKind.DWord);
                registryKey.SetValue(
                    ManuallySelectedEnginesValueName,
                    ManuallySelectedEngines.Select(debuggerEngine => debuggerEngine.ID).ToArray(),
                    RegistryValueKind.MultiString);
            }
        }

        public void RestoreSelectionFromRegistry()
        {
            using (var registryKey = Registry.CurrentUser.CreateSubKey(RegistryKeyPath))
            {
                var valueNames = registryKey.GetValueNames();

                if (valueNames.Contains(IsAutomaticValueName))
                    IsAutomatic = ((int)registryKey.GetValue(IsAutomaticValueName)) != 0;
                else
                    IsAutomatic = true;

                string[] manuallySelectedEngineIds = new string[0];
                if (valueNames.Contains(ManuallySelectedEnginesValueName))
                {
                    manuallySelectedEngineIds = (string[])registryKey.GetValue(ManuallySelectedEnginesValueName);
                }

                foreach (var debuggerEngine in ManualSelection)
                {
                    debuggerEngine.IsSelected = manuallySelectedEngineIds.Contains(debuggerEngine.ID, StringComparer.OrdinalIgnoreCase);
                }
            }
        }

        public bool IsManual // for WPF binding
        {
            get
            {
                return !m_isAutomatic;
            }
            set
            {
                if (m_isAutomatic != !value)
                {
                    m_isAutomatic = !value;

                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsAutomatic)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsManual)));
                }
            }
        }

        public bool IsLazy => m_isLazy;

        public IReadOnlyList<DebuggerEngine> ManualSelection => m_manualSelection;

        public IEnumerable<DebuggerEngine> ManuallySelectedEngines => m_manualSelection.Where(debuggerEngine => debuggerEngine.IsSelected);

        public event PropertyChangedEventHandler PropertyChanged;

        public override string ToString()
        {
            return IsAutomatic ? "Automatic: " + DefaultDebuggerEngineName : string.Join(", ", ManuallySelectedEngines);
        }

        public DebuggerEngines Clone()
        {
            return new DebuggerEngines(
                m_isLazy,
                m_isAutomatic,
                m_manualSelection
                    .Select(debuggerEngine => debuggerEngine.Clone()).ToList());
        }
    }

    public sealed class DebuggerEngine : INotifyPropertyChanged, IComparable<DebuggerEngine>
    {
        private bool m_isSelected;

        public DebuggerEngine(string name, string id, bool isSelected)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            ID = id ?? throw new ArgumentNullException(nameof(id));
            IsSelected = isSelected;
        }

        public string Name { get; }

        public string ID { get; }

        public bool IsSelected
        {
            get
            {
                return m_isSelected;
            }
            set
            {
                m_isSelected = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public override string ToString()
        {
            return Name;
        }

        public DebuggerEngine Clone()
        {
            return new DebuggerEngine(Name, ID, IsSelected);
        }

        public int CompareTo(DebuggerEngine other)
        {
            if (other == null)
                return 1;

            return StringComparer.OrdinalIgnoreCase.Compare(Name, other.Name);
        }
    }

    public class SelectDebuggerEngineDialogModel : ISelectDebuggerEngineDialogModel, INotifyPropertyChanged
    {
        public SelectDebuggerEngineDialogModel(DebuggerEngines debuggerEngines)
        {
            DebuggerEngines = debuggerEngines?.Clone() ?? throw new ArgumentNullException(nameof(debuggerEngines));

            DebuggerEngines.PropertyChanged += DebuggerEngines_PropertyChanged;
        }

        public DebuggerEngines DebuggerEngines { get; set; }

        public bool SelectionValid
        {
            get
            {
                return DebuggerEngines.IsAutomatic || DebuggerEngines.ManuallySelectedEngines.Any();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void DebuggerEngines_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectionValid)));
        }
    }

    public sealed class SelectDebuggerEngineDialogModelDesignSample : ISelectDebuggerEngineDialogModel
    {
        public static readonly DebuggerEngine[] SampleEngines =
        {
            new DebuggerEngine("Managed (v4.6, v4.5, v4.0) code", "ID0", isSelected: true),
            new DebuggerEngine("Native", "ID1", isSelected: true),
            new DebuggerEngine("ABC", "ID2", isSelected: false),
            new DebuggerEngine("DEF", "ID3", isSelected: false),
            new DebuggerEngine("GHI", "ID4", isSelected: false),
            new DebuggerEngine("JKL", "ID5", isSelected: false),
            new DebuggerEngine("MNO", "ID6", isSelected: false),
            new DebuggerEngine("PQR", "ID7", isSelected: false),
            new DebuggerEngine("STU", "ID8", isSelected: false),
            new DebuggerEngine("VWX", "ID9", isSelected: false),
            new DebuggerEngine("YZ0", "ID10", isSelected: false),
        };

        public DebuggerEngines DebuggerEngines { get; set; } = new DebuggerEngines(isAutomatic: false, manualSelection: SampleEngines);

        public bool SelectionValid => false;
    }
}
