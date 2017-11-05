import { HubConnection } from '@aspnet/signalr-client';
import * as signalR from '@aspnet/signalr-client';
import { Subject } from 'rxjs/Subject';

export class App {
  public content: Subject<string> = new Subject<string>();
  public code: Subject<string> = new Subject<string>();
  public connected: Subject<void> = new Subject<void>();

  private connection: HubConnection;

  public connect(): void {
    this.openConnection();
  }

  public sendCode(value: string): void {
    console.log(value);
    this.connection.invoke('connectWithCode', value);
  }

  public createGroup(): void {
    this.connection.invoke('createGroup');
  }

  public sendContent(content: string): void {
    this.connection.send('sendContent', content);
  }

  private openConnection() {
    this.startConnection('/presenter', (connection: HubConnection) => {
      // Create a function that the hub can call to broadcast messages.
      connection.on('broadcastContent', (content) => {
        console.log(content);
        this.content.next(content);
      });

      connection.on('connected', (content) => {
        console.log(content);
        this.connected.next();
      });

      connection.on('connectedToGroup', (groupname, username) => {
        console.log('group: ' + groupname + ', user: ' + username);
      });

      connection.on('createdGroup', (groupname, code) => {
        console.log('group: ' + groupname + ', code: ' + code);
        this.code.next(code);
      });

      connection.on('error', (errMessage) => {
        console.log(errMessage);
      });
    })
      .then((connection: HubConnection) => {
        console.log('connection started');
        console.log(connection);
        this.connection = connection;
      })
      .catch((error: any) => {
        console.error(error.message);
      });
  }

  // Starts a connection with transport fallback - if the connection cannot be started using
  // the webSockets transport the function will fallback to the serverSentEvents transport and
  // if this does not work it will try longPolling. If the connection cannot be started using
  // any of the available transports the function will return a rejected Promise.
  private startConnection(url: string, configureConnection: any) {
    return function start(transport: signalR.TransportType): Promise<HubConnection> {
      console.log(`Starting connection using ${signalR.TransportType[transport]} transport`);
      const connection = new HubConnection(url, { transport });
      if (configureConnection && typeof configureConnection === 'function') {
        configureConnection(connection);
      }
      return connection.start()
        .then(() => {
          return connection;
        })
        .catch((error: any) => {
          console.log(`Cannot start the connection use
                  ${signalR.TransportType[transport]} transport. ${error.message}`);

          if (transport !== signalR.TransportType.LongPolling) {
            return start(transport + 1);
          }
          return Promise.reject(error);
        });
    }(signalR.TransportType.WebSockets);
  }
}
