using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Speech.Recognition;
using System.Speech.Synthesis;
using Microsoft.Win32;
using Forms = System.Windows.Forms;

public sealed class Jarvis {
    readonly string root = AppDomain.CurrentDomain.BaseDirectory;
    readonly Window win;
    readonly TextBox entry;
    readonly TextBlock caption, phase;
    readonly MediaElement film;
    readonly Image poster;
    readonly Button action, mic, mute;
    readonly DispatcherTimer tick = new DispatcherTimer();
    readonly MediaPlayer voice = new MediaPlayer();
    SpeechRecognitionEngine recognizer;
    string state = "idle", project;
    bool silent, startup, engaged, closing, returnToClient;
    DateTime lastProgress;
    TimeSpan lastPosition;
    readonly DispatcherTimer handoff=new DispatcherTimer { Interval=TimeSpan.FromSeconds(5) };
    int cue = -1;
    readonly string[] prompts = { "你好贾维斯", "贾维斯", "启动系统", "继续启动", "现在几点", "打开工作台", "导出素材", "重新播放" };
    readonly double[] times = { 0, 5.4, 10.4, 14.6 };
    readonly string[] subtitles = { "正在扫描装甲轮廓。", "反应堆输出稳定。", "通讯链路已接通。", "工作台就绪，等你指令。" };
    public Jarvis(string workspace) {
        project = workspace;
        using(var f = File.OpenRead(Path.Combine(root,"window.xaml"))) win = (Window)XamlReader.Load(f);
        entry = Find<TextBox>("entry"); caption = Find<TextBlock>("caption"); phase = Find<TextBlock>("phase");
        film = Find<MediaElement>("film"); poster = Find<Image>("poster");
        mic = Find<Button>("mic"); mute = Find<Button>("mute"); action = Find<Button>("action");
        Poster("standby.jpg");
        Click("action",() => Command(state=="paused" ? "继续启动" : (state=="idle" || (startup && state=="playing") ? "你好贾维斯" : (state=="playing" ? "跳过" : "启动系统"))));
        Click("workspace", OpenProject); Click("export", Export); Click("mic", Listen);
        Click("mute",() => { silent=!silent; film.IsMuted=silent; voice.IsMuted=silent; mute.Content=silent?"开启声音":"静音"; });
        Click("send",Submit);
        entry.KeyDown += (s,e) => { if(e.Key==Key.Enter) { Submit(); e.Handled=true; } };
        film.MediaEnded += (s,e) => { if(state=="playing") Ready(); };
        film.MediaFailed += (s,e) => { Ready(); caption.Text="动画播放失败。你仍可进入工作台或导出素材。"; };
        tick.Interval=TimeSpan.FromMilliseconds(100);
        tick.Tick += (s,e) => {
            var pos=film.Position;
            if(pos!=lastPosition){lastPosition=pos;lastProgress=DateTime.UtcNow;}
            if(startup && DateTime.UtcNow-lastProgress>TimeSpan.FromSeconds(10)){Ready();return;}
            int i=Array.FindLastIndex(times,x=>x<=pos.TotalSeconds);if(i>=0 && i!=cue){cue=i;caption.Text=subtitles[i];}
        };
        handoff.Tick+=(s,e)=>{handoff.Stop();if(!engaged)OpenProject();};
        entry.PreviewMouseDown+=(s,e)=>Engage();
        entry.TextChanged+=(s,e)=>{if(entry.Text.Length>0)Engage();};
        win.Closed += (s,e) => { closing=true;handoff.Stop();tick.Stop();film.Close();voice.Close(); if(recognizer!=null){recognizer.RecognizeAsyncCancel();recognizer.Dispose();recognizer=null;} };
    }
    void BeginStartup(){
        startup=true;win.Title="J.A.R.V.I.S. 互动开机";
        win.WindowStyle=WindowStyle.None;win.ResizeMode=ResizeMode.NoResize;win.WindowState=WindowState.Maximized;
        Find<Button>("workspace").Content=returnToClient?"返回 AI 客户端  ·  Esc":"进入工作台  ·  Esc";
        win.PreviewKeyDown+=(s,e)=>{if(e.Key==Key.Escape){e.Handled=true;OpenProject();}};
        win.Loaded+=(s,e)=>Play();
    }
    void Engage(){
        if(!startup || closing)return;
        engaged=true;handoff.Stop();
        if(state=="playing"){
            film.Pause();tick.Stop();state="paused";action.Content="继续启动";
            phase.Text="VOICE LINK  /  STANDBY";caption.Text="等待指令";
        }
    }
    T Find<T>(string name) where T:class {return (T)win.FindName(name);}
    void Click(string name, Action f){ Find<Button>(name).Click+=(s,e)=>f(); }
    string Asset(string file){return Path.Combine(root,"assets",file);}
    void Poster(string file){poster.Source=new BitmapImage(new Uri(Asset(file)));}
    void Submit(){var text=entry.Text; entry.Clear(); Command(text);}
    void Say(string text,string wav){
        caption.Text=text;voice.Stop();
        if(!silent && wav!=null){voice.Open(new Uri(Asset(wav)));voice.Play();}
    }
    public void Command(string text){
        string q=new string(text.ToLowerInvariant().Where(c=>char.IsLetterOrDigit(c)).ToArray());
        if(q.Length==0 || closing)return;
        Engage();
        if(new[]{"你好贾维斯","贾维斯","嗨贾维斯","hijarvis","jarvis","heyjarvis"}.Contains(q)){
            if(state=="playing"){caption.Text="我在。开机简报进行中。";return;}
            if(state!="paused")state="awake";phase.Text="VOICE LINK  /  ONLINE";action.Content=state=="paused"?"继续启动":"启动系统";
            Say("我在。","awake.wav");return;
        }
        if(state=="paused" && new[]{"启动系统","继续启动","继续","开始","启动"}.Contains(q)){
            voice.Stop();state="playing";cue=-1;lastProgress=DateTime.UtcNow;
            phase.Text="ARC SEQUENCE  /  ACTIVE";action.Content="你好，贾维斯";film.Play();tick.Start();return;
        }
        if(new[]{"启动系统","继续启动","继续","开始","开机","重新播放","启动"}.Contains(q)){Play();return;}
        if(q=="跳过" && state=="playing"){Ready();return;}
        if(new[]{"打开工作台","进入工作台","打开codex"}.Contains(q)){OpenProject();return;}
        if(q=="导出素材"){Export();return;}
        if(new[]{"现在几点","几点了","时间"}.Contains(q)){Say("现在是 "+DateTime.Now.ToString("HH:mm")+"。",null);return;}
        caption.Text="可以说「启动系统」「现在几点」或「打开工作台」。";
    }
    void Play(){
        voice.Stop();film.Stop();state="playing";cue=-1;phase.Text="ARC SEQUENCE  /  ACTIVE";action.Content=startup?"你好，贾维斯":"跳过动画";
        lastProgress=DateTime.UtcNow;lastPosition=TimeSpan.Zero;
        film.Visibility=Visibility.Visible;film.Source=new Uri(Asset("boot.mp4"));film.Position=TimeSpan.Zero;film.IsMuted=silent;film.Play();tick.Start();
    }
    void Ready(){
        tick.Stop();film.Stop();film.Visibility=Visibility.Hidden;Poster("online.jpg");state="ready";phase.Text="WORKSPACE  /  READY";action.Content="重播简报";caption.Text=subtitles[3];
        if(startup && !engaged){
            caption.Text=returnToClient?"开机完成，5 秒后返回。点「说话」可继续互动。":"工作台就绪，5 秒后进入。点「说话」可继续互动。";
            handoff.Start();
        }
    }
    void OpenProject(){
        if(returnToClient){win.Close();return;}
        if(startup){
            if(closing)return;closing=true;handoff.Stop();tick.Stop();film.Stop();
            Say("正在打开你的工作台。","open.wav");
            var enter=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(silent?1:1650)};
            enter.Tick+=(s,e)=>{enter.Stop();if(!win.IsVisible)return;
                try{CodexDesktop.Open();win.Close();}
                catch(Exception ex){closing=false;engaged=true;Ready();caption.Text="打开失败："+ex.Message;}
            };enter.Start();return;
        }
        if(!Find<Button>("workspace").IsEnabled)return;
        if(String.IsNullOrEmpty(project)){
            using(var f=new Forms.FolderBrowserDialog()){f.Description="选择要在 Codex 中打开的工作区";if(f.ShowDialog()!=Forms.DialogResult.OK)return;project=f.SelectedPath;}
        }
        try {
            var directory=new DirectoryInfo(project);
            if(!directory.Exists)throw new DirectoryNotFoundException(project);
            // A working directory plus a constant argument keeps paths out of the command line.
            var cli=(Environment.GetEnvironmentVariable("PATH")??"").Split(Path.PathSeparator)
                .Select(p=>p.Trim().Trim('"')).Where(Path.IsPathRooted)
                .Select(p=>Path.Combine(p,"codex.exe")).FirstOrDefault(File.Exists);
            if(cli==null)throw new FileNotFoundException("请先安装 Codex CLI 并加入 PATH。");
            var start=new ProcessStartInfo(cli,"app ."){ WorkingDirectory=directory.FullName,UseShellExecute=false,CreateNoWindow=true };
            var proc=new Process {StartInfo=start,EnableRaisingEvents=true};
            proc.Exited+=(s,e)=>win.Dispatcher.BeginInvoke(new Action(()=>{
                int code=proc.ExitCode;proc.Dispose();
                if(code==0){
                    var finish=new DispatcherTimer {Interval=TimeSpan.FromSeconds(silent?.1:2.6)};
                    finish.Tick+=(sender,ev)=>{finish.Stop();win.Close();};finish.Start();
                }else {Find<Button>("workspace").IsEnabled=true;caption.Text="Codex 未能打开此工作区，退出码 "+code+"。";}
            }));
            if(!proc.Start())throw new IOException("无法运行 Codex。");
            Find<Button>("workspace").IsEnabled=false;Say("正在打开你的工作台。","open.wav");
        } catch(Exception ex){caption.Text="打开工作台失败："+ex.Message;}
    }
    void Export(){
        Engage();
        var save=new SaveFileDialog {FileName="JARVIS-剪辑素材.zip",Filter="ZIP 素材包|*.zip",OverwritePrompt=true};
        if(save.ShowDialog(win)!=true)return;
        try{File.Copy(Asset("edit-kit.zip"),save.FileName,true);caption.Text="已保存到 "+save.FileName;}
        catch(Exception e){caption.Text="保存失败："+e.Message;}
    }
    void Listen(){
        if(recognizer!=null)return;
        Engage();
        try{
            var info=SpeechRecognitionEngine.InstalledRecognizers().FirstOrDefault(r=>r.Culture.Name.StartsWith("zh"));
            if(info==null)throw new InvalidOperationException("Windows 未安装中文语音识别。");
            var rec=new SpeechRecognitionEngine(info);recognizer=rec;
            var grammar=new GrammarBuilder {Culture=info.Culture};grammar.Append(new Choices(prompts));rec.LoadGrammar(new Grammar(grammar));
            rec.InitialSilenceTimeout=TimeSpan.FromSeconds(5);rec.BabbleTimeout=TimeSpan.FromSeconds(4);
            rec.RecognizeCompleted+=(s,e)=>win.Dispatcher.BeginInvoke(new Action(()=>{
                if(recognizer!=rec || closing)return;
                var result=e.Error==null && !e.Cancelled ? e.Result:null;
                recognizer=null;rec.Dispose();mic.IsEnabled=true;mic.Content="🎙  说话";
                if(result!=null && result.Confidence>=.48)Command(result.Text);else caption.Text="没听清。再点一次说话，或在下方输入。";
            }));
            rec.SetInputToDefaultAudioDevice();voice.Stop();rec.RecognizeAsync(RecognizeMode.Single);
            mic.IsEnabled=false;mic.Content="聆听中…";caption.Text="正在听。请说「你好贾维斯」「启动系统」或「打开工作台」。";
        }catch(Exception ex){if(recognizer!=null)recognizer.Dispose();recognizer=null;mic.IsEnabled=true;caption.Text="可改用文字或按钮。"+ex.Message;}
    }
    [STAThread] public static int Main(string[] args){
        try{
            bool standalone=args.Contains("--return-to-client") || !args.Contains("--open-chatgpt");
            if(!args.Contains("--console") && !args.Contains("--awake") && !args.Contains("--project") && !args.Contains("--choose")){
                var boot=new Jarvis(null){returnToClient=standalone};
                boot.BeginStartup();boot.win.ShowDialog();return 0;
            }
            string cwd=Environment.CurrentDirectory;
            bool explicitProject=args.Contains("--project");
            int i=Array.IndexOf(args,"--project");
            if(i>=0 && i+1<args.Length)cwd=Path.GetFullPath(args[i+1]);
            bool same=String.Equals(cwd.TrimEnd('\\'),AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\'),StringComparison.OrdinalIgnoreCase);
            var app=new Jarvis(!explicitProject && (same || args.Contains("--choose")) ? null : cwd);
            app.returnToClient=standalone;
            if(standalone)app.Find<Button>("workspace").Content="返回 AI 客户端";
            if(args.Contains("--awake"))app.win.Loaded+=(s,e)=>app.Command("你好贾维斯");
            app.win.ShowDialog();return 0;
        }catch(Exception ex){MessageBox.Show(ex.Message,"J.A.R.V.I.S.");return 1;}
    }
}
