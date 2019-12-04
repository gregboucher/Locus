using System.Collections.Generic;

namespace Locus.Models
{
    public interface IRepository
    {
        Asset GetAsset(int SerialNumber);
    }
}