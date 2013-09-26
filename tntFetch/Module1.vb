Imports System.Net
Imports System.Text
Imports System.IO

Module Module1

    Sub Main()
        GetFinancialSummaries()
    End Sub


    Sub GetFinancialSummaries()
        Dim d As New agapeconnectDataContext


        Dim countries = From c In d.AP_mpd_Countries

        Dim tuPass = (From c In d.AP_StaffBroker_Settings Where c.PortalId = 0 And c.SettingName = "TrustedUserPassword" Select c.SettingValue).First



        Dim service = "https://www.agapeconnect.me" ' "https://tntdataserver.com/dataserver/mkd/" ' country.AP_StaffBroker_Settings.Where(Function(c) c.SettingName = "DataserverURL" And c.PortalId = country.portalId).First.SettingValue


        Dim restServer As String = "https://thekey.me/cas/v1/tickets/"

        Dim postData = "service=" & service & "&username=trusteduser@agapeconnect.me&password=" & tuPass

        Dim request As WebRequest = WebRequest.Create(restServer)
        Dim byteArray As Byte() = Encoding.UTF8.GetBytes(postData)
        request.ContentLength = byteArray.Length
        request.Method = "POST"
        request.ContentType = "application/x-www-form-urlencoded"

        Dim datastream As Stream = request.GetRequestStream
        datastream.Write(byteArray, 0, byteArray.Length)
        datastream.Close()
        Dim response As WebResponse = request.GetResponse
        restServer = response.Headers.GetValues("Location").ToArray()(0)

        ' Dim tgt = location.Substring(location.LastIndexOf("/") + 1)



        For Each country In countries
            Try


               
                Dim t As New tnt.TntMPDDataServerWebService2
                Dim tntURL = country.AP_StaffBroker_Settings.Where(Function(c) c.SettingName = "DataserverURL" And c.PortalId = country.portalId).First.SettingValue

                t.Url = tntURL & "dataquery/dataqueryservice2.asmx"
                t.Discover()

                postData = "service=" & tntURL
                request = WebRequest.Create(restServer)
                byteArray = Encoding.UTF8.GetBytes(postData)
                request.ContentLength = byteArray.Length
                request.Method = "POST"
                request.ContentType = "application/x-www-form-urlencoded"

                datastream = request.GetRequestStream
                datastream.Write(byteArray, 0, byteArray.Length)
                datastream.Close()

                response = request.GetResponse

                response.Headers.GetValues("location")
                datastream = response.GetResponseStream
                Dim reader As New StreamReader(datastream)
                Dim ST = reader.ReadToEnd()

                Dim sessionId = t.Auth_Login(tntURL, ST, False).SessionID

                Dim myInfo = t.MyWebUser_GetInfo(sessionId)
                Dim allInfo = t.WebUser_GetAllInfo(sessionId)
                ' Dim allInfo = t.WebUser_GetAllInfo(sessionId)
                If (myInfo.StaffProfiles.Where(Function(c) c.Code = "All Accounts").Count = 0) Then
                    t.WebUserMgmt_AddStaffProfile(sessionId, "4EA08A9D-1D66-5BBD-71A4-6F4C59FAE37E", "All Accounts", "All Financial Accounts")
                    t.WebUserMgmt_StaffProfile_AddFinancialAccount(sessionId, "4EA08A9D-1D66-5BBD-71A4-6F4C59FAE37E", "All Accounts", "", True, "")

                End If

                'For Each Staff
                'get past 14 months data (all of it)

                'Parse and iterate the lines..
                Dim startDate = New Date(Today.AddMonths(-14).Year, Today.AddMonths(-14).Month, 1)
                Dim EndDate = New Date(Today.AddMonths(1).Year, Today.AddMonths(1).Month, 1).AddDays(-1)

                Console.Write("StartDate: " & startDate.ToString("dd MMM yyyy") & vbNewLine)
                Console.Write("EndDate: " & EndDate.ToString("dd MMM yyyy") & vbNewLine)
                Dim allAccountInfo = t.StaffPortal_GetFinancialTransactions(sessionId, "All Accounts", startDate, EndDate, "", False)

                For Each staff In country.AP_mpdCalc_Definition.AP_mpdCalc_StaffBudgets.Select(Function(c) c.AP_StaffBroker_Staff).Distinct

                    Console.Write("Processing: " & staff.DisplayName & vbNewLine)

                    Dim RC = staff.CostCenter

                    Dim AccountInfo = From c In allAccountInfo.FinancialAccounts Where c.Code.Trim = RC.Trim


                    Dim Trx = From c In allAccountInfo.FinancialTransactions Where c.FinancialAccountCode.Trim = RC.Trim
                        Group By y = c.TransactionDate.Year, m = c.TransactionDate.Month Into Group
                        Select y, m, Income = Group.Where(Function(x) x.GLAccountIsIncome).Sum(Function(x) x.Amount),
                        Expense = Group.Where(Function(x) Not x.GLAccountIsIncome).Sum(Function(x) x.Amount),
                        Foreign = Group.Where(Function(x) x.GLAccountIsIncome And x.GLAccountCode.StartsWith("50")).Sum(Function(x) x.Amount),
                        Allocation = Group.Where(Function(x) x.GLAccountIsIncome And x.GLAccountCode.StartsWith("57")).Sum(Function(x) x.Amount),
                        Turnover = Group.Sum(Function(x) x.Amount)
                        Order By y, m
                    Dim Bal = AccountInfo.First.BeginningBalance
                    Console.Write("StartBal: " & Bal & vbNewLine)



                    For Each mon In Trx
                        Dim period = New Date(mon.y, mon.m, 1).ToString("yyyyMM")
                        Console.Write(period & vbNewLine)
                        Bal += mon.Turnover
                        Dim summary = From c In d.AP_mpd_UserAccountInfos Where c.mpdCountryId = country.mpdCountryId And c.period = period And c.staffId = staff.StaffId

                        If summary.Count = 0 Then
                            Dim insert As New AP_mpd_UserAccountInfo
                            insert.staffId = staff.StaffId
                            insert.balance = Bal
                            insert.period = period
                            insert.mpdCountryId = country.mpdCountryId
                            insert.expense = mon.Expense
                            insert.compensation = mon.Allocation
                            insert.foreignIncome = mon.Foreign
                            insert.income = mon.Income
                            d.AP_mpd_UserAccountInfos.InsertOnSubmit(insert)

                        Else
                            summary.First.balance = Bal
                            summary.First.income = mon.Income
                            summary.First.expense = mon.Expense
                            summary.First.foreignIncome = mon.Foreign
                            summary.First.compensation = mon.Allocation

                        End If


                    Next
                    Console.Write("EndBal: " & AccountInfo.First.EndingBalance & " vs " & Bal & vbNewLine)

                    d.SubmitChanges()
                Next




                


                '  Dim allInfo = t.WebUser_GetAllInfo(sessionId)




                Console.Write("Finished!")
                Console.ReadKey()
            Catch ex As Exception
                Console.Write(ex.ToString)
                Console.ReadKey()
            End Try
        Next




    End Sub

End Module
