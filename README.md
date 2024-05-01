# serverSharp
webserver written in C#.

# server output

![image](https://github.com/equqe/serverSharp/assets/145790372/ff1df39d-2bea-49fa-af5a-00b5a40b3d79)

# page render

![image](https://github.com/equqe/serverSharp/assets/145790372/c0dabb6d-6f9b-4121-b2d6-d9ad1c0785ea)

![image](https://github.com/equqe/serverSharp/assets/145790372/c4cef193-d705-4424-8052-252cde1cf8ff)



# notes
server uses `System.Globalization;` for determining the correct local time of the host:

```
DateTime currentDateTime = DateTime.Now;
   string formattedDateTime = currentDateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
```

you can change the port by changing `int port` variable. the port is set to 5050 by default:

```
private static int port = 5050;
```
