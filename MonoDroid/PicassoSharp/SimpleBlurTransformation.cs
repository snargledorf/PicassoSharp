using System;
using Android.Content;
using Android.Graphics;
using Android.Renderscripts;

namespace PicassoSharp
{
    public class SimpleBlurTransformation : ITransformation<Bitmap>
    {
        private readonly int m_BlurRadius;
        private RenderScript m_Rs;

        public SimpleBlurTransformation(Context context, int blurRadius)
        {
            if (blurRadius > 25)
                throw new ArgumentOutOfRangeException("blurRadius", "Blur radius must be within 0 - 25");

            // Create the Renderscript instance that will do the work.
            m_Rs = RenderScript.Create(context);

            m_BlurRadius = blurRadius;
        }

        public Bitmap Transform(Bitmap source)
        {
            // Create another bitmap that will hold the results of the filter.
            Bitmap blurredBitmap = Bitmap.CreateBitmap(source);

            // Allocate memory for Renderscript to work with
            Allocation input = Allocation.CreateFromBitmap(m_Rs, source, Allocation.MipmapControl.MipmapFull, AllocationUsage.Script);
            Allocation output = Allocation.CreateTyped(m_Rs, input.Type);

            // Load up an instance of the specific script that we want to use.
            ScriptIntrinsicBlur script = ScriptIntrinsicBlur.Create(m_Rs, Element.U8_4(m_Rs));
            script.SetInput(input);

            // Set the blur radius
            script.SetRadius(m_BlurRadius);

            // Start the ScriptIntrinisicBlur
            script.ForEach(output);

            // Copy the output to the blurred bitmap
            output.CopyTo(blurredBitmap);

            if (blurredBitmap != source)
                source.Recycle();

            return blurredBitmap;
        }

        public string Key { get { return "SimpleBlurTransformation"; } }
    }
}