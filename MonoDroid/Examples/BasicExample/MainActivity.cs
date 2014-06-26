using Android.App;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Widget;
using Android.OS;
using PicassoSharp;

namespace BasicExample
{
    [Activity(Label = "BasicExample", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        private const string TestImagePath =
            "http://upload.wikimedia.org/wikipedia/commons/thumb/d/d7/Android_robot.svg/511px-Android_robot.svg.png";

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            ImageView imageView = FindViewById<ImageView>(Resource.Id.ImageView);

            // Get our button from the layout resource,
            // and attach an event to it
            Button button = FindViewById<Button>(Resource.Id.MyButton);

            button.Click += delegate
            {
                Picasso.With(this)
                    .Load(TestImagePath)
                    .SkipCache()
                    .Into(imageView);
            };
        }
    }
}

