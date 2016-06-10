﻿function OnCategoriesDocumentReady() {
    $('#nestable').nestable({}).on('change', OnCategoriesStructureChanged);
    $("#NewCategoryFormSubmit").click(SendNewCategoryForm);
    $("#EditCategoryButton").click(OnEditCategoryClick);
    jQuery.extend(jQuery.validator.messages, {
        required: "Заполните поле"
    });
    $('.colorInput').colorpicker();
}

function OnEditCategoryDocumentReady() {
    $('.colorInputCategoryEdit').colorpicker();
    $("#EditCategoryFormSubmit").click(SendEditCategoryForm);
}

function SendEditCategoryForm() {
    var form = document.getElementById("EditCategoryForm");
    var formData = new FormData(form);
    var file = document.getElementById("EditCategoryIconInput").files[0];
    formData.append("categoryIcon", file);

    var file = document.getElementById("EditCategoryPinInput").files[0];
    formData.append("categoryPin", file);

    $.ajax({
        url: "Dictionaries/EditCategory",
        data: formData,
        contentType: false,
        processData: false,
        type: "POST",
        success: function() {
            AddNewCategorySuccess();
            $("#Modal").modal("hide");
        },
        error:function() {
            ViewNotification("Ошибка","error");
        }
    });
    return false;
}

function OnEditCategoryClick() {
    var categoryId = $("#EditCategorySelect").val();
    $.ajax({
        url: "Dictionaries/_EditCategoryModalPartial",
        type: "POST",
        data:{categoryId:categoryId},
        success: function (data) {
            $("#ModalContent").html(data);
            $("#Modal").modal("show");
            OnEditCategoryDocumentReady();
        },
        error: function () {
            ViewNotification('Ошибка', 'error');
        }
    });
}

function SendNewCategoryForm() {
    if (!($("#NewCategoryForm").valid())) {
        ViewNotification("Заполните обязательные поля!", "error");
        return 0;
    }
    var form = document.getElementById("NewCategoryForm");
    var formData = new FormData(form);
    var file = document.getElementById("NewCategoryIconInput").files[0];
    formData.append("categoryIcon", file);
    file = document.getElementById("NewCategoryPinInput").files[0];
    formData.append("categoryPin", file);
    $.ajax({
        url: "Dictionaries/AddNewCategory",
        data: formData,
        contentType: false,
        processData: false,
        type: "POST",
        success: AddNewCategorySuccess,
        error: function() {
            ViewNotification("Ошибка", "error");
        }
    });
    return false;
}

function RefreshCategoriesPage() {
    $.ajax({
        url: "Dictionaries/_CategoriesPartial",
        type: "POST",
        success: function (data) {
            $("#CategoriesContainer").html(data);
            OnCategoriesDocumentReady();
        },
        error: function () {
            ViewNotification('Ошибка', 'error');
        }
    });
}

function AddNewCategorySuccess() {
    ViewNotification("Категория сохранена", "success");
    RefreshCategoriesPage();
}

function OnCategoriesStructureChanged(e) {
    var list = e.length ? e : $(e.target);
    var string = window.JSON.stringify(list.nestable('serialize'));
    SendNewStructure(string);
}

function SendNewStructure(string) {
    var url = "Dictionaries/SaveCategoriesStructure";
    $.ajax({
        url: url,
        type: "POST",
        data: { structure: string },
        success: function () {
            ViewNotification("Изменения сохранены", 'success');
        },
        error: function () {
            ViewNotification("Не удалось сохранить", 'error');
        }
    });
}

$(document).ready(OnCategoriesDocumentReady);