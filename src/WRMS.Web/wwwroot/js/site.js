document.addEventListener("DOMContentLoaded", function () {
    var toggleBtn = document.getElementById("sidebarToggleBtn");
    var sidebar = document.getElementById("appSidebar");
    if (toggleBtn && sidebar) {
        toggleBtn.addEventListener("click", function () {
            sidebar.classList.toggle("show");
        });
    }

    var collapseBtn = document.getElementById("sidebarCollapseBtn");
    if (collapseBtn) {
        collapseBtn.addEventListener("click", function () {
            var collapsed = document.documentElement.classList.toggle("sidebar-collapsed");
            localStorage.setItem("wrms-sidebar-collapsed", collapsed ? "1" : "0");
        });
    }

    document.addEventListener("click", function (e) {
        if (!sidebar || window.innerWidth > 991) return;
        if (sidebar.contains(e.target) || (toggleBtn && toggleBtn.contains(e.target))) return;
        sidebar.classList.remove("show");
    });

    document.querySelectorAll("form[data-confirm]").forEach(function (form) {
        form.addEventListener("submit", function (e) {
            var message = form.getAttribute("data-confirm") || "Are you sure?";
            if (!confirm(message)) {
                e.preventDefault();
            }
        });
    });

    document.querySelectorAll("button[data-confirm]").forEach(function (button) {
        button.addEventListener("click", function (e) {
            var message = button.getAttribute("data-confirm") || "Are you sure?";
            if (!confirm(message)) {
                e.preventDefault();
            }
        });
    });
});
