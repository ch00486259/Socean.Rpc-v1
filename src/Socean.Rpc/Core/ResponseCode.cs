namespace Socean.Rpc.Core
{
    public enum ResponseCode : byte
    {
        OK = 0,
        SERVER_INTERNAL_ERROR = 200,
        SERVICE_NOT_FOUND = 201,
        SERVICE_TITLE_ERROR = 202,
        FILTER_INTERNAL_ERROR = 210,
    }
}
