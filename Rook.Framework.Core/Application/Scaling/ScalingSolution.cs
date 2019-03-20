namespace Rook.Framework.Core.Application.Scaling
{
    internal class ScalingSolution
    {
            public ScalingSolution() { }
            public ScalingSolution(string response, bool status)
            {
                Response = response;
                Status = status;
            }
            public string Response { get; set; }
            public bool Status { get; set; }
    }
}