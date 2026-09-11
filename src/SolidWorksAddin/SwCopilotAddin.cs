using System;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swpublished;
using SwCursor.SolidWorksAddin.Services;
using SwCursor.SolidWorksAddin.UI;

namespace SwCursor.SolidWorksAddin
{
    [ComVisible(true)]
    [Guid("8B107B9C-15E8-42F7-A338-D58DA2F266B2")]
    [ProgId("SwCursor.AICadCopilot")]
    public sealed class SwCopilotAddin : ISwAddin
    {
        private const string AddinTitle = "Mechra";
        private SldWorks _swApp;
        private TaskpaneView _taskpane;
        private CopilotPanel _panel;
        private AgentClient _agent;
        private CadExecutor _executor;

        public bool ConnectToSW(object ThisSW, int Cookie)
        {
            _swApp = (SldWorks)ThisSW;
            _swApp.SetAddinCallbackInfo2(0, this, Cookie);

            _agent = new AgentClient();
            var context = new ModelContextService(_swApp);
            _executor = new CadExecutor(_swApp, context);
            _panel = new CopilotPanel(context, _agent, _executor);
            _panel.CreateControl();

            _taskpane = (TaskpaneView)_swApp.CreateTaskpaneView2(string.Empty, AddinTitle);
            _taskpane.DisplayWindowFromHandlex64(_panel.Handle.ToInt64());
            return true;
        }

        public bool DisconnectFromSW()
        {
            try { _taskpane?.DeleteView(); } catch { }
            try { _panel?.Dispose(); } catch { }
            try { _agent?.Dispose(); } catch { }
            _taskpane = null;
            _panel = null;
            _agent = null;
            _executor = null;
            _swApp = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return true;
        }

        [ComRegisterFunction]
        public static void Register(Type t)
        {
            string guid = "{" + t.GUID.ToString().ToUpperInvariant() + "}";
            using (var addin = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\SOLIDWORKS\Addins\" + guid))
            {
                addin.SetValue(null, 1, RegistryValueKind.DWord);
                addin.SetValue("Title", AddinTitle);
                addin.SetValue("Description", "Mechra - AI-native mechanical design copilot for SOLIDWORKS");
            }
            using (var startup = Registry.CurrentUser.CreateSubKey(@"Software\SOLIDWORKS\AddInsStartup\" + guid))
                startup.SetValue(null, 1, RegistryValueKind.DWord);
        }

        [ComUnregisterFunction]
        public static void Unregister(Type t)
        {
            string guid = "{" + t.GUID.ToString().ToUpperInvariant() + "}";
            try { Registry.LocalMachine.DeleteSubKeyTree(@"SOFTWARE\SOLIDWORKS\Addins\" + guid, false); } catch { }
            try { Registry.CurrentUser.DeleteSubKeyTree(@"Software\SOLIDWORKS\AddInsStartup\" + guid, false); } catch { }
        }
    }
}
