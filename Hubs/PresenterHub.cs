using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using System.Text;

public class PresenterHub : Hub
{
  // stores code and group name
  private Dictionary<string, string> codeStore;
  // connectionId -> groupname
  private Dictionary<string, string> groups;  
  private readonly static int[] codeCharacters = Enumerable.Range('a', 26).Concat(Enumerable.Range('0', 10)).ToArray();

  public PresenterHub(PresenterService presenterService)
  {
    this.codeStore = presenterService.CodeStore;
    this.groups = presenterService.Groups;
  }
  public async Task ConnectWithCode(string code)
  {
    if (this.codeStore.ContainsKey(code))
    {
      var groupName = this.codeStore.GetValueOrDefault(code);
      if (!string.IsNullOrWhiteSpace(groupName))
      {
        await this.Groups.AddAsync(this.Context.ConnectionId, groupName);
        // await this.Clients.Client(this.Context.ConnectionId).InvokeAsync("connectedToGroup", groupName, );
        await this.Clients.Group(groupName).InvokeAsync("connectedToGroup", groupName, this.Context.User.Identity.Name);
        return;
      }
    }
    await this.Clients.Client(this.Context.ConnectionId).InvokeAsync("error", "no group found");
  }

  public async Task CreateGroup()
  {
    string code = string.Empty;
    do
    {
      code = GenerateCode(4);
    } while (this.codeStore.ContainsKey(code));

    var groupName = this.Context.User.Identity.Name ?? "1234-abc";
    await this.Groups.AddAsync(this.Context.ConnectionId, groupName);
    this.codeStore.Add(code, groupName);
    this.groups.Add(this.Context.ConnectionId, groupName);

    await this.Clients.Client(this.Context.ConnectionId).InvokeAsync("createdGroup", groupName, code);
  }

  public async Task SendContent(string content)
  {
    string groupName = this.groups.GetValueOrDefault(this.Context.ConnectionId);
    if (!string.IsNullOrEmpty(groupName))
    {
      string decodedContent = System.Net.WebUtility.HtmlDecode(content);
      await this.Clients.Group(groupName).InvokeAsync("broadcastContent", decodedContent);
    }
  }

  public override async Task OnConnectedAsync()
  {
    await this.Clients.Client(this.Context.ConnectionId).InvokeAsync("connected", this.Context.User.Identity.Name);
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