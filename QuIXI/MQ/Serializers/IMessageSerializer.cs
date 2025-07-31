
namespace QuIXI.MQ.Serializers
{
    public interface IMessageSerializer
    {
        byte[] Serialize<T>(T obj);
        T Deserialize<T>(byte[] data);
    }
}
