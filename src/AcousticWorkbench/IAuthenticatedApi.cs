// <copyright file="IAuthenticatedApi.cs" company="QutEcoacoustics">
// All code in this file and all associated files are the copyright and property of the QUT Ecoacoustics Research Group (formerly MQUTeR, and formerly QUT Bioacoustics Research Group).
// </copyright>

namespace AcousticWorkbench
{
    public interface IAuthenticatedApi : IApi
    {
        string Username { get; }

        string Token { get; }
    }
}