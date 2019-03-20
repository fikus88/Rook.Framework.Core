namespace Rook.Framework.Core.Services
{
    /// <summary>
    /// Any IStartable implementations get round up on <seealso cref="IService.Start"/> and are triggered in order of <see cref="StartupPriority"/>
    /// </summary>
    public interface IStartable
    {
        /// <summary>
        /// What should happen when starting up
        /// </summary>
        void Start();

        /// <summary>
        /// Which order it should be ran in, lowest numbers first
        /// </summary>
        StartupPriority StartupPriority { get; }
    }




}
