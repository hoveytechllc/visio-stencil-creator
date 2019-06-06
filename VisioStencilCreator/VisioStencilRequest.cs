using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace VisioStencilCreator
{
    public class VisioStencilRequest
    {
        /// <summary>
        /// Absolute file paths of images to be used
        /// </summary>
        public IList<string> ImageFilePaths { get; } = new List<string>();
    }
}
