namespace PicassoSharp
{
    public interface IRequestTransformer<T>
    {
        Request<T> TransformRequest(Request<T> request);
    }
}