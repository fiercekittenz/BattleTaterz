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
   public static double MoveTimerDefault = 7.0;

   public bool IsWarning { get; set; } = false;

   public Godot.Color DefaultColor { get; set; } = new Godot.Color(167, 144, 234, 1);

   public Godot.Color FlashingColor { get; set; } = new Godot.Color(255, 93, 166);

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
         if (_timer.TimeLeft > 5)
         {
            int rounded = (int)Math.Ceiling(_timer.TimeLeft);
            Text = $"{rounded}";
         }
         else
         {
            IsWarning = true;
            Warn();
            Text = string.Format("{0:N2}", _timer.TimeLeft);
         }
      }
   }

   public void Update()
   {
      if (_timer.TimeLeft > 5)
      {
         int rounded = (int)Math.Ceiling(_timer.TimeLeft);
         Text = $"{rounded}";
      }
      else
      {
         IsWarning = true;
         Warn();
         Text = string.Format("{0:N2}", _timer.TimeLeft);
      }
   }

   public void Warn()
   {
      //if (!IsWarning)
      //{
      //   Tween flashing = GetTree().CreateTween();
      //   flashing.SetParallel(true);
      //   flashing.TweenProperty(this, "theme_override_colors/font_color", FlashingColor, 1.0f).SetEase(Tween.EaseType.InOut);
      //   flashing.TweenProperty(this, "modulate:a", 0.0f, 0.2f).SetEase(Tween.EaseType.InOut);
      //   flashing.Finished += (() =>
      //   {
      //      // Clean up!
      //      ResetAppearance();
      //      flashing.Kill();
      //   });
      //}
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
      Set("theme_override_colors/font_color", DefaultColor);
      Set("modulate:a", 1.0f);
      IsWarning = false;
   }

   private void _timer_Timeout()
   {
      throw new NotImplementedException();
   }

   #endregion

   #region Private Members

   // Reference to the move timer on the game board.
   private Timer _timer;

   // Denotes if the label has been initialized or not. Should only be done once.
   private bool _isInitialized = false;

   #endregion
}
