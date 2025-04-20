using BattleTaterz.Core.UI;
using Godot;
using System;
using System.Threading.Tasks;

/// <summary>
/// Label that actively displays the countdown timer for a move.
/// </summary>
public partial class MoveTimerLabel : Label
{
   #region Properties

   /// <summary>
   /// Default time for a move. Serves as the "starting" value and may change based on levels.
   /// </summary>
   public static double MoveTimerDefault = 20.0;

   public static double WarnThreshold = 5.0;

   public bool IsWarning { get; set; } = false;

   public Godot.Color DefaultColor { get; set; } = new Godot.Color(167, 144, 234, 1);

   public Godot.Color FlashingColor { get; set; } = new Godot.Color(255, 93, 166);

   /// <summary>
   /// An event that can be listened to for when the timer has run out.
   /// </summary>
   public event EventHandler OnTimerFinished;

   #endregion

   #region Methods

   public void Initialize(Timer timer)
   {
      if (!_isInitialized)
      {
         _timer = timer;

         _timer.Timeout += _timer_Timeout;

         _isInitialized = true;
      }
   }

   public void Start()
   {
      _timer.WaitTime = MoveTimerLabel.MoveTimerDefault; // seconds
      _timer.Start();
   }

   public override void _Process(double delta)
   {
      if (_isInitialized && _timer != null && !_timer.Paused)
      {
         int rounded = (int)Math.Ceiling(_timer.TimeLeft);

         if (_timer.TimeLeft >= WarnThreshold)
         {
            Text = $"{rounded}";
         }
         else
         {
            if (!IsWarning)
            {
               Warn();
               IsWarning = true;
            }

            if (_timer.TimeLeft >= 1 && _timer.TimeLeft < WarnThreshold)
            {
               Text = string.Format($"{rounded}", _timer.TimeLeft);
            }
            else
            {
               Text = string.Format("{0:N2}", _timer.TimeLeft);
            }
         }
      }
   }

   public void Warn()
   {
      if (!IsWarning)
      {         
         Tween flashing = GetTree().CreateTween();
         flashing.SetParallel(false);
         flashing.TweenProperty(this, "modulate:a", 0.0f, 0.5f).SetEase(Tween.EaseType.Out);
         flashing.TweenProperty(this, "modulate:a", 1.0f, 0.5f).SetEase(Tween.EaseType.Out);
         flashing.SetLoops(2);

         flashing.Finished += (() =>
         {
            // Clean up!
            flashing.Stop();
            flashing.Kill();
         });
      }
   }

   public void ResetTime()
   {
      ResetAppearance();

      _timer.Stop();
      _timer.WaitTime = MoveTimerLabel.MoveTimerDefault;
      _timer.Start();
   }

   public void ResetAppearance()
   {
      Set("modulate:a", 1.0f);
      IsWarning = false;
   }

   private void _timer_Timeout()
   {
      ResetAppearance();
      OnTimerFinished?.Invoke(this, new EventArgs());
   }

   #endregion

   #region Private Members

   // Reference to the move timer on the game board.
   private Timer _timer;

   // Reference to the tween that will run when the warning period is hit.
   private Tween _flashing;

   // Denotes if the label has been initialized or not. Should only be done once.
   private bool _isInitialized = false;

   #endregion
}
