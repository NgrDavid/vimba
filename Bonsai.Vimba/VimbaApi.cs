using System;

namespace Bonsai.Vimba
{
    using Vimba = AVT.VmbAPINET.Vimba;

    static class VimbaApi
    {
        static readonly Lazy<Vimba> handle = new Lazy<Vimba>(() => new Vimba());

        public static Vimba Handle
        {
            get { return handle.Value; }
        }
    }
}
