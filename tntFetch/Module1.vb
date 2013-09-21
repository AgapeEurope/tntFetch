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



        Dim service = "https://tntdataserver.com/dataserver/mkd/" ' country.AP_StaffBroker_Settings.Where(Function(c) c.SettingName = "DataserverURL" And c.PortalId = country.portalId).First.SettingValue


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

                postData = "service=" & service
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

                Dim sessionId = t.Auth_Login(service, ST, False).SessionID

                Dim myInfo = t.MyWebUser_GetInfo(sessionId)
                Dim allInfo = t.WebUser_GetAllInfo(sessionId)
                ' Dim allInfo = t.WebUser_GetAllInfo(sessionId)
                If (myInfo.StaffProfiles.Where(Function(c) c.Code = "All Accounts").Count = 0) Then
                    t.WebUserMgmt_AddStaffProfile(sessionId, "4EA08A9D-1D66-5BBD-71A4-6F4C59FAE37E", "All Accounts", "All Financial Accounts")
                    t.WebUserMgmt_StaffProfile_AddFinancialAccount(sessionId, "4EA08A9D-1D66-5BBD-71A4-6F4C59FAE37E", "All Accounts", "", True, "")

                End If




                For i As Integer = -12 To -1
                    Dim startDate = New Date(Today.AddMonths(i).Year, Today.AddMonths(i).Month, 1)
                    Dim EndDate = startDate.AddMonths(1).AddDays(-1)
                    Dim allAccountInfo = t.StaffPortal_GetFinancialTransactions(sessionId, "All Accounts", startDate, EndDate, "", False)

                    For Each staff In country.AP_mpdCalc_Definition.AP_mpdCalc_StaffBudgets
                        Dim RC = staff.AP_StaffBroker_Staff.CostCenter
                        Dim AccountInfo = From c In allAccountInfo.FinancialAccounts Where c.Code.Trim = RC.Trim

                        Dim Trx = From c In allAccountInfo.FinancialTransactions Where c.FinancialAccountCode.Trim = RC.Trim

                        If AccountInfo.Count > 0 Then
                            Dim bal = AccountInfo.First.EndingBalance

                            Dim income = (From c In Trx Where c.GLAccountIsIncome Select c.Amount).Sum
                            Dim ForeignIncome = (From c In Trx Where c.GLAccountIsIncome And c.Code.Trim.StartsWith("41") Select c.Amount).Sum
                            Dim Expense = (From c In Trx Where Not c.GLAccountIsIncome Select c.Amount).Sum
                            Dim summary = From c In d.AP_mpd_UserAccountInfos Where c.mpdCountryId = country.mpdCountryId And c.period = startDate.ToString("yyyyMM") And c.staffId = staff.StaffId

                            If summary.Count = 0 Then
                                Dim insert As New AP_mpd_UserAccountInfo
                                insert.staffId = staff.StaffId
                                insert.balance = bal
                                insert.period = startDate.ToString("yyyyMM")
                                insert.mpdCountryId = country.mpdCountryId
                                insert.expense = Expense
                                insert.compensation = 0
                                insert.foreignIncome = ForeignIncome
                                insert.income = income
                                d.AP_mpd_UserAccountInfos.InsertOnSubmit(insert)

                            Else
                                summary.First.balance = bal
                                summary.First.income = income
                                summary.First.expense = Expense
                                summary.First.foreignIncome = ForeignIncome
                                summary.First.compensation = 0

                            End If

                        End If





                    Next

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
