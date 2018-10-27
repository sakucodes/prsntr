using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using System.Text;

public class PresenterHub : Hub
{
  // stores code and group name
  private Dictionary<string, string> _codeStore;
  // connectionId -> groupname
  private Dictionary<string, string> _groups;  
  private readonly static int[] codeCharacters = Enumerable.Range('a', 26).Concat(Enumerable.Range('0', 10)).ToArray();

  public PresenterHub(PresenterService presenterService)
  {
    _codeStore = presenterService.CodeStore;
    _groups = presenterService.Groups;
  }
  public async Task ConnectWithCode(string code)
  {
    if (_codeStore.ContainsKey(code))
    {
      var groupName = _codeStore.GetValueOrDefault(code);
      if (!string.IsNullOrWhiteSpace(groupName))
      {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        // await this.Clients.Client(this.Context.ConnectionId).InvokeAsync("connectedToGroup", groupName, );
        await Clients.Group(groupName).SendAsync("connectedToGroup", groupName, Context.User.Identity.Name);
        return;
      }
    }
    await Clients.Client(Context.ConnectionId).SendAsync("error", "no group found");
  }

  public async Task CreateGroup()
  {
    string code = string.Empty;
    do
    {
      code = GenerateCode(4);
    } while (_codeStore.ContainsKey(code));

    var groupName = Context.User.Identity.Name ?? "1234-abc";
    await Groups.AddToGroupAsync(this.Context.ConnectionId, groupName);
    _codeStore.Add(code, groupName);
    _groups.Add(Context.ConnectionId, groupName);

    await Clients.Client(Context.ConnectionId).SendAsync("createdGroup", groupName, code);
  }

  public async Task SendContent(string content)
  {
    string groupName = _groups.GetValueOrDefault(Context.ConnectionId);
    if (!string.IsNullOrEmpty(groupName))
    {
      string decodedContent = System.Net.WebUtility.HtmlDecode(content);
      await Clients.Group(groupName).SendAsync("broadcastContent", decodedContent);
    }
  }

  public override async Task OnConnectedAsync()
  {
    await Clients.Client(Context.ConnectionId).SendAsync("connected", Context.User.Identity.Name);
  }

  private string GenerateCode(int length)
  {
    var random = new Random();
    StringBuilder sb = new StringBuilder();
    for (int i = 0; i < length; i++)
    {
      sb.Append((char)codeCharacters[random.Next(codeCharacters.Count())]);
    }
    return sb.ToString();
  }
}