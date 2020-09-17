import QtQuick 2.12
import QtQuick.Window 2.12
import QtQuick.Layouts 1.12

import QtQuick.Controls 1.4

Window {
    visible: true
    width: 400
    height: 640
    title: "Toggl Track"

    function mixColors(a, b, ratio) {
        return Qt.rgba(
            ratio * a.r + (1.0 - ratio) * b.r,
            ratio * a.g + (1.0 - ratio) * b.g,
            ratio * a.b + (1.0 - ratio) * b.b,
            ratio * a.a + (1.0 - ratio) * b.a,
        )
    }

    Rectangle {
        id: mainWindow
        anchors.fill: parent


        function setAlpha(color, alpha) {
            return Qt.rgba(color.r, color.g, color.b, alpha)
        }

        SystemPalette {
            id: mainPalette
            property bool isDark: (itemShadow.r + itemShadow.g + itemShadow.b) < 1

            property int itemShadowSize: mainPalette.isDark ? 1 : 9
            property color itemShadow: mixColors(mainPalette.shadow, mainPalette.listBackground, 0.2)
            property color listBackground: mixColors(mainPalette.base, mainPalette.alternateBase, 0.8)

            property color borderColor: mixColors(mainPalette.text, mainPalette.base, 0.33)
        }
        SystemPalette {
            id: disabledPalette
            colorGroup: SystemPalette.Disabled
        }

        TextMetrics {
            id: termsAndConditionsMetrics
            font.pointSize: 9
            text: "I agree to terms of service and privacy policy"
        }

        Connections {
            target: toggl
            onDisplayLogin: {
                mainView.source = "LoginView.qml"
                timeEntryEdit.visible = false
            }
            onDisplayTimeEntryList: {
                console.warn("HU")
                mainView.source = "TimeEntryListView.qml"
                timeEntryEdit.visible = false
            }
            onDisplayTimeEntryEditor: {
                timeEntryEdit.timeEntry = view
                timeEntryEdit.visible = true
            }
        }

        Loader {
            id: mainView
            anchors.fill: parent
            source: toggl.status === "MAIN TAB BAR VIEW" ? "TimeEntryListView.qml" : toggl.status === "LOGIN VIEW" ? "LoginView.qml" : null
        }

        TimeEntryEditView {
            id: timeEntryEdit
            visible: false
            anchors.fill: parent
        }

        ErrorBubble {
            anchors.bottom: parent.bottom
            anchors.right: parent.right
            anchors.margins: 16
            maximumWidth: parent.width - 32
        }
    }
}