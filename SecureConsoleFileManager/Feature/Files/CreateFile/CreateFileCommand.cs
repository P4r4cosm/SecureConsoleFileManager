using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using SecureConsoleFileManager.Models;

namespace SecureConsoleFileManager.Feature.Files.CreateFile
{
    public record class CreateFileCommand(string CommandArgument): IRequest<MbResult>
    {
        
    }
}