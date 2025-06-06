using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Linq;
using WixSharp.UI.Forms;
using WixToolset.Dtf.WindowsInstaller;
using WixToolset.Mba.Core;

using io = System.IO;
using sys = System.Windows.Forms;

namespace WixSharp
{
    //public class ClrDialogs
    //{
    //    static Type WelcomeDialog = typeof(WelcomeDialog);
    //    static Type LicenceDialog = typeof(LicenceDialog);
    //    static Type FeaturesDialog = typeof(FeaturesDialog);
    //    static Type InstallDirDialog = typeof(InstallDirDialog);
    //    static Type ExitDialog = typeof(ExitDialog);

    //    static Type RepairStartDialog = typeof(RepairStartDialog);
    //    static Type RepairExitDialog = typeof(RepairExitDialog);

    //    static Type ProgressDialog = typeof(ProgressDialog);
    //}
#pragma warning disable 1591

    public static class UIExtensions
    {
        public static string GetVersion(this System.Reflection.Assembly assembly)
            => assembly.GetName().Version.ToString();

        public static System.Drawing.Icon GetAssiciatedIcon(this string extension)
        {
            var dummy = Path.GetTempPath() + extension;
            System.IO.File.WriteAllText(dummy, "");
            var result = System.Drawing.Icon.ExtractAssociatedIcon(dummy);
            System.IO.File.Delete(dummy);
            return result;
        }

        internal static sys.Control ForceAutoScale(this sys.Control control)
        {
            var graphics = control.CreateGraphics();
            float scalingFactor = graphics.DpiY / 96; //96 DPI corresponds to 100% scaling
            control.Scale(new SizeF(scalingFactor, scalingFactor));
            return control;
        }

        internal static float GetCurrentScaling(this sys.Control control)
        {
            var graphics = control.CreateGraphics();
            float scalingFactor = graphics.DpiY / 96; //96 DPI corresponds to 100% scaling
            return scalingFactor;
        }

        public static sys.Control CustomScale(this sys.Control control, float scalingFactor)
        {
            control.Scale(new SizeF(scalingFactor, scalingFactor));
            return control;
        }

        /// <summary>
        /// MsiRuntime object associated with the ManagedUI shell.
        /// </summary>
        /// <param name="shell">The shell.</param>
        /// <returns></returns>
        public static MsiRuntime MsiRuntime(this IManagedUIShell shell)
        {
            return shell.RuntimeContext as MsiRuntime;
        }

        public static void GoNext(this IShellView view) => view.Shell.GoNext();

        public static void GoTo<T>(this IShellView view) => view.Shell.GoTo<T>();

        /// <summary>
        /// MsiRuntime object associated with the ManagedUI dialog.
        /// </summary>
        /// <param name="dialog">The dialog.</param>
        /// <returns></returns>
        public static MsiRuntime MsiRuntime(this IManagedDialog dialog)
        {
            return dialog.Shell.RuntimeContext as MsiRuntime;
        }

        /// <summary>
        /// Session object associated with the ManagedUI dialog.
        /// </summary>
        /// <param name="dialog">The dialog.</param>
        /// <returns></returns>
        public static Session Session(this IManagedDialog dialog)
        {
            return dialog.Shell.MsiRuntime().MsiSession;
        }

        public static bool IsReadOnly(this TreeNode node)
        {
            return node is ReadOnlyTreeNode r_node && r_node.IsReadOnly;
        }

        public static bool IsModified(this TreeNode node)
        {
            return node is ReadOnlyTreeNode r_node && r_node.Checked != r_node.DefaultChecked;
        }

        public static void ResetCheckedToDefault(this TreeNode node, int delay = -1)
        {
            try
            {
                if (node is ReadOnlyTreeNode _node && _node.IsReadOnly)
                {
                    if (delay == -1)
                        node.Checked = _node.DefaultChecked;
                    else
                        ThreadPool.QueueUserWorkItem(x =>
                        {
                            Thread.Sleep(delay);

                            node.TreeView.BeginInvoke((MethodInvoker)delegate
                            {
                                node.Checked = _node.DefaultChecked;
                            });
                        });
                }
            }
            catch
            {
                // Some collisions can happen since this method is called multiple times in succession.
                // It supposed to fix the TreeView UX limitations.
                // Ignoring the exception does not create any problem but only (potentially) will require
                // user to do an extra mouse-click to ensure checked state of the checkebox.
            }
        }

        public static void MakeTransparentOn(this Label label, PictureBox pictureBox)
        {
            label.Parent = pictureBox;
            label.Location = new Point(label.Location.X - pictureBox.Location.X, label.Location.Y - pictureBox.Location.Y);
            label.BackColor = Color.Transparent;
        }

        /// <summary>
        /// The use modern folder browser dialog that is themed and has a new look consistent with modern Windows theme.
        /// <p>Since .NET does not provide API for this dialog the COM Interop needs to be used instead.</p>
        /// <remarks>Use of COM Interop can have unexpected side effects at runtime so tis option is disabled by default and
        /// the old style <see cref="System.Windows.Forms.FolderBrowserDialog"/> used instead. However as soon as the field usage
        /// statistics can show the stability of COM Interop approach it will be made the default option instead.</remarks>
        /// </summary>
        public static bool UseModernFolderBrowserDialog(this ISession session) => ((Session)session.SessionContext).UseModernFolderBrowserDialog();

        public static sys.Control ClearChildren(this sys.Control control)
        {
            foreach (sys.Control item in control.Controls)
                item.Dispose();

            control.Controls.Clear();
            return control;
        }

#pragma warning restore 1591

        //NOT RELIABLE
        //The detection of the installdir is not deterministic. For example 'Shortcuts' sample has
        //three logical installdirs INSTALLDIR, DesktopFolder and ProgramMenuFolder. The INSTALLDIR
        //is the real one that we need to discover but there is no way to understand its role by analyzing
        //the MSI tables. And the other problem is that we cannot rely on its name as user can overwrite it.
        //WIX solves this problem by requiring the user explicitly link the installdir ID to the WIXUI_INSTALLDIR
        //property: <Property Id="WIXUI_INSTALLDIR" Value="INSTALLDIR"  />.
        //public static string GetInstallDirectoryName(this Session session)
        //{
        //    List<Dictionary<string, object>> result = session.OpenView("select * from Component");
        //
        //    var dirs = result.Select(x => x["Directory_"]).Cast<string>().Distinct().ToArray();
        //
        //    string shortestDir = dirs.Select(x => new { Name = x, Parts = session.GetDirectoryPathParts(x) })
        //                             .OrderBy(x => x.Parts.Length)
        //                             .Select(x => x.Name)
        //                             .FirstOrDefault();
        //    if (shortestDir == null)
        //        throw new Exception("GetInstallDirectoryPath Error: cannot find InstallDirectory");
        //    else
        //        return shortestDir;
        //}

        /// <summary>
        /// Gets the target system directory path based on specified directory name (MSI Directory table).
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        public static string GetDirectoryPath(this Session session, string name)
        {
            string[] subDirs = session.GetDirectoryPathParts(name)
                                      .Select(x => x.AsWixVarToPath())
                                          .ToArray();
            return string.Join(@"\", subDirs);
        }

        static string[] GetDirectoryPathParts(this Session session, string name)
        {
            var path = new List<string>();
            var names = new Queue<string>(new[] { name });

            while (names.Any())
            {
                var item = names.Dequeue();

                using (var sql = session.Database.OpenView("select * from Directory where Directory = '" + item + "'"))
                {
                    sql.Execute();
                    using (var record = sql.Fetch())
                    {
                        if (record != null)
                        {
                            // [<internal_id>|]<name>[:<source_name>]
                            var subDir = record.GetString("DefaultDir").Split('|').Last().Split(':').First();
                            path.Add(subDir);

                            if (!record.IsNull("Directory_Parent"))
                            {
                                var parent = record.GetString("Directory_Parent");
                                if (parent != "TARGETDIR")
                                    names.Enqueue(parent);
                            }
                        }
                    }
                }
            }
            path.Reverse();
            return path.ToArray();
        }

        internal static string UserOrDefaultContentOf(string extenalFilePath, string srcDir, string outDir, string fileName, object defaultContent)
        {
            string extenalFile = Utils.PathCombine(srcDir, extenalFilePath);

            if (extenalFilePath.IsNotEmpty()) //important to test before PathComibed
                return extenalFile;

            var file = Path.Combine(outDir, fileName);

            if (defaultContent is byte[])
                io.File.WriteAllBytes(file, (byte[])defaultContent);
            else if (defaultContent is Bitmap)
                ((Bitmap)defaultContent).Save(file, ImageFormat.Png);
            else if (defaultContent is string)
                io.File.WriteAllBytes(file, ((string)defaultContent).GetBytes());
            else if (defaultContent == null)
                return "<null>";
            else
                throw new Exception("Unsupported ManagedUI resource type.");

            Compiler.TempFiles.Add(file);
            return file;
        }

        /// <summary>
        /// Safe version of <see cref="WixToolset.Mba.Core.IEngine.Apply(IntPtr)"/>. It allows passing
        /// parent handle <c>null</c> or <c>IntPtr.Zero</c> to allow applying the "setup plan" when the bootstrapper
        /// window is not available. In such case SafeApply will use the handle returned by the <see cref="GetForegroundWindow()"/>
        /// </summary>
        /// <param name="engine">The engine.</param>
        /// <param name="hwndParent">The HWND parent.</param>
        static public void SafeApply(this IEngine engine, IntPtr? hwndParent = null) => engine.Apply(GetForegroundWindow());

        [DllImport("User32.dll")]
        static extern IntPtr GetForegroundWindow();

        /// <summary>
        /// Localizes the control its contained <see cref="T:System.Windows.Forms.Control.Text"/> from the specified localization
        /// delegate 'localize'.
        /// <para>The method substitutes both localization file (*.wxl) entries and MSI properties contained by the input string
        /// with their translated/converted values.</para>
        /// <remarks>
        /// Note that both localization entries and MSI properties must be enclosed in the square brackets
        /// (e.g. "[ProductName] Setup", "[InstallDirDlg_Title]").
        /// </remarks>
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="localize">The localize.</param>
        /// <returns></returns>
        public static sys.Control LocalizeWith(this sys.Control control, Func<string, string> localize)
        {
            var controls = new Queue<sys.Control>(new[] { control });

            while (controls.Any())
            {
                var item = controls.Dequeue();

                item.Text = item.Text.LocalizeWith(localize);

                item.Controls
                .OfType<sys.Control>()
                .ForEach(x => controls.Enqueue(x));
            }
            return control;
        }

        static Regex locRegex = new Regex(@"\[.+?\]");
        static Regex cleanRegex = new Regex(@"{\\(.*?)}"); //removes font info "{\WixUI_Font_Bigger}Welcome to the [ProductName] Setup Wizard"

        /// <summary>
        /// Localizes the string from the specified localization delegate 'localize'.
        /// <para>The method substitutes both localization file (*.wxl) entries and MSI properties contained by the input string
        /// with their translated/converted values.</para>
        /// <remarks>
        /// Note that both localization entries and MSI properties must be enclosed in the square brackets
        /// (e.g. "[ProductName] Setup", "[InstallDirDlg_Title]").
        /// <code>
        /// Func&lt;string, string&gt; localizer = e.ManagedUI.Shell.MsiRuntime().Localize;
        /// var localizedText =  "[ProductName] Setup".LocalizeWith(localizer);
        /// </code>
        /// </remarks>
        /// </summary>
        /// <param name="textToLocalize">The text to localize.</param>
        /// <param name="localize">The localize.</param>
        /// <returns></returns>
        public static string LocalizeWith(this string textToLocalize, Func<string, string> localize)
        {
            if (textToLocalize.IsEmpty()) return textToLocalize;

            var result = new StringBuilder(textToLocalize);

            //first rum will replace all UI constants, which in turn may contain MSI properties to resolve.
            //second run will resolve properties if any found.
            for (int i = 0; i < 2; i++)
            {
                string text = result.ToString();
                result.Length = 0; //clear

                int lastEnd = 0;
                foreach (Match match in locRegex.Matches(text))
                {
                    result.Append(text.Substring(lastEnd, match.Index - lastEnd));
                    lastEnd = match.Index + match.Length;

                    string key = match.Value.Trim('[', ']');

                    result.Append(localize(key));
                }

                if (lastEnd != text.Length)
                    result.Append(text.Substring(lastEnd, text.Length - lastEnd));
            }
            return cleanRegex.Replace(result.ToString(), "");
        }

        /// <summary>
        /// Plays the specified dialogs in demo mode.
        /// </summary>
        /// <param name="dialogs">The dialogs.</param>
        public static void Play(this ManagedDialogs dialogs)
        {
            UIShell.Play(dialogs);
        }

        /// <summary>
        /// Plays the install dialogs in demo mode.
        /// </summary>
        /// <param name="ui">The UI.</param>
        public static void PlayInstallDialogs(this IManagedUI ui) => UIShell.Play(ui.InstallDialogs);

        /// <summary>
        /// Plays the modify dialogs in demo mode.
        /// </summary>
        /// <param name="ui">The UI.</param>
        public static void PlayModifyDialogs(this IManagedUI ui) => UIShell.Play(ui.ModifyDialogs);
    }
}