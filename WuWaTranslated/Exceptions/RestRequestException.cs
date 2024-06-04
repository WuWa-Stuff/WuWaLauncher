using RestSharp;

namespace WuWaTranslated.Exceptions;

public class RestRequestException : Exception
{
    public readonly RestRequest Request;
    public readonly RestResponse Response;

    public RestRequestException(string message, RestRequest request, RestResponse response) : base(message)
    {
        Request = request;
        Response = response;
    }
}